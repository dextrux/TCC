using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using CoreDomain.Scripts.Services.CommandFactory;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands {
    public class DisposeLevelCommand : BaseCommand, ICommandVoid {
        private ILevelCancellationTokenService _levelCancellationTokenService;
        //private IGameInputActionsController _gameInputActionsController;

        private bool _shouldReleaseFromMemory;

        public DisposeLevelCommand SetShouldReleaseAssetsFromMemory(bool shouldReleaseFromMemory) {
            _shouldReleaseFromMemory = shouldReleaseFromMemory;
            return this;
        }

        public override void ResolveDependencies() {
            _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
            //_gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
        }

        public void Execute() {
            _levelCancellationTokenService.CancelCancellationToken();
            //_gameInputActionsController.UnregisterAllInputListeners();
        }
    }
}
