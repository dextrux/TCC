using System.Threading;
using CoreDomain.GameDomain.Scripts.States.LobbyState;
using CoreDomain.Scripts.CoreInitiator;
using CoreDomain.Scripts.CoreInitiator.Base;
using CoreDomain.Scripts.Mvc.LoadingScreen;
using CoreDomain.Scripts.Services.InitiatorInvokerService;
using CoreDomain.Scripts.Services.SceneService;
using CoreDomain.Scripts.Services.StateMachineService;
using CoreDomain.Scripts.Utils;
using UnityEngine;

namespace CoreDomain.GameDomain.Scripts.GameInitiator {
    public class GameInitiator : ISceneInitiator, IGameInitiator {
        private readonly IStateMachineService _stateMachine;
        private readonly ILoadingScreenController _loadingScreenController;
        private readonly LobbyState.Factory _lobbyStateFactory;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;

        public ScenesType SceneType => ScenesType.GameScene;

        public GameInitiator(IStateMachineService stateMachine, LobbyState.Factory LobbyStateFactory, ILoadingScreenController loadingScreenController, ISceneInitiatorsService sceneInitiatorsService) {
            _stateMachine = stateMachine;
            _lobbyStateFactory = LobbyStateFactory;
            _loadingScreenController = loadingScreenController;
            _sceneInitiatorsService = sceneInitiatorsService;
            _sceneInitiatorsService.RegisterInitiator(this);
        }

        public async Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            var enterData = (GameInitiatorEnterData)enterDataObject;
            _ = _loadingScreenController.SetLoadingSlider(0.5f, cancellationTokenSource);
            //To-Do colocar um load real assincono do level
            //await _levelsDataService.LoadLevelsData(cancellationTokenSource);
            await _stateMachine.EnterInitialGameState(_lobbyStateFactory.Create(new LobbyInitiatorEnterData()), cancellationTokenSource);
        }

        public Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            return AwaitableUtils.CompletedTask;
        }

        public Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource) {
            _sceneInitiatorsService.UnregisterInitiator(this);
            return AwaitableUtils.CompletedTask;
        }
    }
}