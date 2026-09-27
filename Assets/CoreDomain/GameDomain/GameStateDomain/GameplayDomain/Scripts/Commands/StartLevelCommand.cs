using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands {
    public class StartLevelCommand : BaseCommand, ICommandAsync {
        private LoadLevelCommandData _commandData;
        //private IGameInputActionsController _gameInputActionsController;
        private IStateMachineService _stateMachineService;
        private ILevelCancellationTokenService _levelCancellationTokenService;
        private IFPSPlayerController _fPSPlayerController;

        private GamePlayInitatorEnterData _enterData;

        public StartLevelCommand SetEnterData(GamePlayInitatorEnterData enterData) {
            _enterData = enterData;
            return this;
        }

        public override void ResolveDependencies() {
            //_gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
            _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            if (_enterData == null || _enterData.SessionMode == NetworkSessionMode.Offline) {
                _fPSPlayerController = _diContainer.Resolve<IFPSPlayerController>();
                _fPSPlayerController.SetUp();
            }

            //_gameInputActionsController.RegisterAllInputListeners();
            //Inserir await para iniciar a fase com algum comando
        }
    }
}