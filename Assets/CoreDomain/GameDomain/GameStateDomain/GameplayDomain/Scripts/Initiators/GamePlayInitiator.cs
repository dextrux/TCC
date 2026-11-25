using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.EntryPoint;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.GameplayNetwork;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.CoreInitiator.Base;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.InitiatorInvokerService;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.SceneService;
using CoreDomain.Scripts.Utils;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiators
{
    /// <summary>
    /// Orquestra o fluxo de entrada e saida da gameplay. Os detalhes de ligacao com a rede
    /// (som e inventario) ficam no GameplayNetworkBridge.
    /// </summary>
    public class GamePlayInitiator : ISceneInitiator, IGamePlayInitiator
    {
        private readonly ICommandFactory _commandFactory;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;
        private readonly IGameplayNetworkBridge _gameplayNetworkBridge;
        private readonly INetworkService _networkService;
        private readonly INoiseManager _noiseManager;
        private readonly ILevelScenarioController _levelScenarioController;
        private readonly IWorldObjectSpawner _worldObjectSpawner;
        private readonly IWorldEnemySpawner _worldEnemySpawner;

        public ScenesType SceneType => ScenesType.GamePlayScene;

        public GamePlayInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService,
            IGameplayNetworkBridge gameplayNetworkBridge,
            INetworkService networkService,INoiseManager noiseManager, ILevelScenarioController levelScenarioController,
            IWorldObjectSpawner worldObjectSpawner, IWorldEnemySpawner worldEnemySpawner)
        {
            _commandFactory = commandFactory;
            _sceneInitiatorsService = sceneInitiatorsService;
            _gameplayNetworkBridge = gameplayNetworkBridge;
            _networkService =  networkService;
            _noiseManager = noiseManager;
            _levelScenarioController = levelScenarioController;
            _worldObjectSpawner = worldObjectSpawner;
            _worldEnemySpawner = worldEnemySpawner;
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
            bool host = enterData.SessionMode == NetworkSessionMode.Host;

            _gameplayNetworkBridge.Configure(offline);

            await _commandFactory
                .CreateCommandAsync<StartGamePlayStateCommand>()
                .SetEnterData(enterData)
                .Execute(cancellationTokenSource);

            // Somente Offline e Host processam a percepção de ruído pela IA.
            if (offline || host)
            {
                _levelScenarioController.CurrentLevelView.Setup(_noiseManager);
            }

            // Somente o Host cria objetos e inimigos.
            if (host)
            {
                _worldObjectSpawner.StartSpawning();
                _worldEnemySpawner.StartSpawning();
            }
            
            if (!offline)
            {
                _networkService.NotifyGameplayReady();
            }
        }

        public async Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource)
        {
            _levelScenarioController.CurrentLevelView.Release();

            _gameplayNetworkBridge.Release();

            _sceneInitiatorsService.UnregisterInitiator(this);
            _commandFactory.CreateCommandVoid<ExitGamePlayStateCommand>().Execute();
            await AwaitableUtils.CompletedTask;
        }
    }
}