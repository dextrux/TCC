using CoreDomain.Scripts.Services.AudioService;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.NetworkService;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.EntryPoint
{
    public class ExitGamePlayStateCommand : BaseCommand, ICommandVoid
    {
        //private IGameInputActionsController _gameInputActionsController;
        private ICommandFactory _commandFactory;
        private IAudioService _audioService;
        private INetworkService _networkService;

        public override void ResolveDependencies()
        {
            //_gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
            _commandFactory = _diContainer.Resolve<ICommandFactory>();
            _networkService = _diContainer.Resolve<INetworkService>();
        }

        public void Execute()
        {
            _networkService.Disconnect();
            _commandFactory.CreateCommandVoid<DisposeLevelCommand>().SetShouldReleaseAssetsFromMemory(true).Execute();
        }
    }
}