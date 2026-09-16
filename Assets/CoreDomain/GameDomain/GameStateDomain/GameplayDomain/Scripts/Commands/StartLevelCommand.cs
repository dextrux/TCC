using System.Threading;
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
        //private ILevelsDataService _levelsDataService;

        public override void ResolveDependencies() {
            //_gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
            _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
            //_levelsDataService = _diContainer.Resolve<ILevelsDataService>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            //_gameInputActionsController.RegisterAllInputListeners();
            //Inserir await para iniciar a fase com algum comando
        }
    }
}
