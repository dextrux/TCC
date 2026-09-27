using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands {
    public class StartLevelCommand : BaseCommand, ICommandAsync {
        private LoadLevelCommandData _commandData;
        //private IGameInputActionsController _gameInputActionsController;
        private IStateMachineService _stateMachineService;
        private ILevelCancellationTokenService _levelCancellationTokenService;
        private IFPSPlayerController _fPSPlayerController;
        private INoiseManager _noiseManager;
        private ILevelScenarioController _levelScenarioController;

        public override void ResolveDependencies() {
            //_gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
            _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
            _fPSPlayerController = _diContainer.Resolve<IFPSPlayerController>();
            _levelScenarioController = _diContainer.Resolve<ILevelScenarioController>();
            _noiseManager = _diContainer.Resolve<INoiseManager>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            _fPSPlayerController.SetUp();
            //_gameInputActionsController.RegisterAllInputListeners();
            //Inserir await para iniciar a fase com algum comando
            _levelScenarioController.CurrentLevelView.Setup(_noiseManager);
        }
    }
}
