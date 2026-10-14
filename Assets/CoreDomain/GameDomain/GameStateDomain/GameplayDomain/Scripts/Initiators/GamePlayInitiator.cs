
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.EntryPoint;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands;
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
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator
{
    public class GamePlayInitiator : ISceneInitiator, IGamePlayInitiator
    {
        private readonly ICommandFactory _commandFactory;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;
        private readonly INetworkPlayerCameraController _networkPlayerCameraController;
        private readonly INetworkSoundService _networkSoundService;
        private readonly IAudioManager _audioManager;
        private readonly INoiseManager _noiseManager;
        private readonly ILevelScenarioController _levelScenarioController;

        public ScenesType SceneType => ScenesType.GamePlayScene;

        public GamePlayInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService, INetworkPlayerCameraController networkPlayerCameraController, INetworkSoundService networkSoundService, IAudioManager audioManager, INoiseManager noiseManager, ILevelScenarioController levelScenarioController)
        {
            _commandFactory = commandFactory;
            _sceneInitiatorsService = sceneInitiatorsService;
            _networkPlayerCameraController = networkPlayerCameraController;
            _networkSoundService = networkSoundService;
            _audioManager = audioManager;
            _noiseManager = noiseManager;
            _levelScenarioController = levelScenarioController;
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

            await _commandFactory.CreateCommandAsync<StartGamePlayStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);

            //Somente Offline e Host processam a percepcao de ruido pela IA.
            if (enterData.SessionMode == NetworkSessionMode.Offline || enterData.SessionMode == NetworkSessionMode.Host)
            {
                _levelScenarioController.CurrentLevelView.Setup(_noiseManager);
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
    }
}
