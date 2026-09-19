using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands {
    public class LoadLevelCommand : BaseCommand, ICommandAsync {
        private IStateMachineService _stateMachineService;
        private ILevelCancellationTokenService _levelCancellationTokenService;

        private LoadLevelCommandData _commandData;

        public LoadLevelCommand SetEnterData(LoadLevelCommandData commandData) {
            _commandData = commandData;
            return this;
        }

        public override void ResolveDependencies() {
            _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            _levelCancellationTokenService.InitCancellationToken();
            int levelNumber = _commandData.LevelNumber;
            await CreateLevel(levelNumber, cancellationTokenSource);
        }

        private async Awaitable CreateLevel(int levelNumber, CancellationTokenSource cancellationTokenSource) {
            //await _levelTrackController.CreateLevelTrack(levelNumber, cancellationTokenSource);
        }
    }
}
