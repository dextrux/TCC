using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.EntryPoint;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.CoreInitiator.Base;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.InitiatorInvokerService;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.SceneService;
using CoreDomain.Scripts.Utils;
using System;
using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface;
using CoreDomain.Scripts.Services.NetworkServices;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator
{
    public class GamePlayInitiator : ISceneInitiator, IGamePlayInitiator
    {
        private readonly ICommandFactory _commandFactory;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;
        private readonly INetworkPlayerCameraController _networkPlayerCameraController;
        private readonly INetworkSoundService _networkSoundService;
        private readonly INetworkInventoryService _networkInventoryService;
        private readonly IInventoryManager _inventoryManager;
        private readonly IAudioManager _audioManager;
        private readonly INoiseManager _noiseManager;
        private readonly ILevelScenarioController _levelScenarioController;
        private readonly IWorldObjectSpawner _worldObjectSpawner;

        public ScenesType SceneType => ScenesType.GamePlayScene;

        public GamePlayInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService, INetworkPlayerCameraController networkPlayerCameraController, INetworkSoundService networkSoundService, IAudioManager audioManager, INoiseManager noiseManager, ILevelScenarioController levelScenarioController, INetworkInventoryService networkInventoryService, IInventoryManager inventoryManager, IWorldObjectSpawner worldObjectSpawner)
        {
            _commandFactory = commandFactory;
            _sceneInitiatorsService = sceneInitiatorsService;
            _networkPlayerCameraController = networkPlayerCameraController;
            _networkSoundService = networkSoundService;
            _audioManager = audioManager;
            _noiseManager = noiseManager;
            _levelScenarioController = levelScenarioController;
            _networkInventoryService = networkInventoryService;
            _inventoryManager = inventoryManager;
            _worldObjectSpawner = worldObjectSpawner;
            _sceneInitiatorsService.RegisterInitiator(this);
        }

        public async Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource)
        {
            GamePlayInitatorEnterData enterData = (GamePlayInitatorEnterData)enterDataObject;
            await _commandFactory.CreateCommandAsync<LoadGamePlayStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);
        }

        public async Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource)
        {
            var enterData = (GamePlayInitatorEnterData)enterDataObject;

            bool offline = enterData.SessionMode == NetworkSessionMode.Offline;

            _networkSoundService.Configure(offline, PlayAudio, ReportNoise);

            // Inventario: o servico de rede nao conhece o IInventoryManager, entao a ligacao eh feita aqui
            _networkInventoryService.Configure(offline, SelectSlot, ScrollSlot, RemoveInventory);
            _inventoryManager.InventoryChanged += OnInventoryChanged;

            await _commandFactory.CreateCommandAsync<StartGamePlayStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);

            //Somente Offline e Host processam a percepcao de ruido pela IA.
            if (enterData.SessionMode == NetworkSessionMode.Offline || enterData.SessionMode == NetworkSessionMode.Host)
            {
                _levelScenarioController.CurrentLevelView.Setup(_noiseManager);
            }

            if (enterData.SessionMode == NetworkSessionMode.Host)
            {
                Debug.Log($"[Initiator] StartSpawning | mode={enterData.SessionMode} | serverActive={Mirror.NetworkServer.active}");
                _worldObjectSpawner.StartSpawning();
            }

            if (enterData.SessionMode != NetworkSessionMode.Offline)
            {
                _networkPlayerCameraController.SetUp();
            }
        }

        public async Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource)
        {
            _levelScenarioController.CurrentLevelView.Release();

            _networkSoundService.Configure(false, null, null);

            _inventoryManager.InventoryChanged -= OnInventoryChanged;
            _networkInventoryService.Configure(false, null, null, null);

            _networkPlayerCameraController.Dispose();
            _sceneInitiatorsService.UnregisterInitiator(this);
            _commandFactory.CreateCommandVoid<ExitGamePlayStateCommand>().Execute();
            await AwaitableUtils.CompletedTask;
        }

        private void PlayAudio(int soundId, Vector3 position, float volumeMultiplier)
        {
            if (!Enum.IsDefined(typeof(AudioEventId), soundId))
            {
                Debug.LogWarning("[GamePlayInitiator] Unknown AudioEventId.");
                return;
            }

            AudioEventId audioId = (AudioEventId)soundId;

            if (audioId == AudioEventId.None)
            {
                return;
            }

            var signal = new AudioSignal(audioId, position, null, volumeMultiplier);
            _audioManager.ReportSignal(signal);
        }

        private void ReportNoise(Vector3 position, float loudness, GameObject source)
        {
            var signal = new NoiseSignal(position, loudness, source);
            _noiseManager.ReportSignal(signal);
        }

        private void SelectSlot(int connectionId, int index)
        {
            _inventoryManager.GetOrCreateInventory(connectionId).Selected(index);
        }

        private void ScrollSlot(int connectionId, int direction)
        {
            _inventoryManager.GetOrCreateInventory(connectionId).ScrollToSelected(direction);
        }

        private void RemoveInventory(int connectionId)
        {
            _inventoryManager.Remove(connectionId);
        }

        private void OnInventoryChanged(int connectionId, PlayerInventory inventory)
        {
            var stack = inventory.SelectedStack;
            _networkInventoryService.NotifyHeldItemChanged(connectionId, stack.IsEmpty() ? 0 : stack.ItemId);
        }
    }
}