using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands {
    public class LoadLevelCommand : BaseCommand, ICommandAsync {
        private IStateMachineService _stateMachineService;
        private ILevelCancellationTokenService _levelCancellationTokenService;
        private ILevelScenarioController _levelScenarioController;

        private LoadLevelCommandData _commandData;

        public LoadLevelCommand SetEnterData(LoadLevelCommandData commandData) {
            _commandData = commandData;
            return this;
        }

        public override void ResolveDependencies() {
            _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
            _levelScenarioController = _diContainer.Resolve<ILevelScenarioController>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            _levelCancellationTokenService.InitCancellationToken();
            string levelTag = _commandData.Leveltag;
            await CreateLevel(levelTag, cancellationTokenSource);
        }

        private async Awaitable CreateLevel(string levelTag, CancellationTokenSource cancellationTokenSource) {
            await _levelScenarioController.CreateLevel(levelTag, cancellationTokenSource);
        }
    }
}
