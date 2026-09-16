using CoreDomain.Scripts.Services.AudioService;
using CoreDomain.Scripts.Services.CommandFactory;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.EntryPoint {
    public class ExitGamePlayStateCommand : BaseCommand, ICommandVoid {
        //private IGameInputActionsController _gameInputActionsController;
        private ICommandFactory _commandFactory;
        private IAudioService _audioService;

        public override void ResolveDependencies() {
            //_gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
            _commandFactory = _diContainer.Resolve<ICommandFactory>();
        }

        public void Execute() {
            _commandFactory.CreateCommandVoid<DisposeLevelCommand>().SetShouldReleaseAssetsFromMemory(true).Execute();
        }
    }
}