using System.Threading;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.AudioService;
using CoreDomain.Scripts.Services.CommandFactory;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands {
    public class StartGamePlayStateCommand : BaseCommand, ICommandAsync {
        private ICommandFactory _commandFactory;
        private IAudioService _audioService;

        private GamePlayInitatorEnterData _enterData;

        public StartGamePlayStateCommand SetEnterData(GamePlayInitatorEnterData enterData) {
            _enterData = enterData;
            return this;
        }

        public override void ResolveDependencies() {
            _commandFactory = _diContainer.Resolve<ICommandFactory>();
            //_audioService = _diContainer.Resolve<IAudioService>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            //_gameInputActionsController.EnableInputs();
            await _commandFactory.CreateCommandAsync<StartLevelCommand>().Execute(cancellationTokenSource);
        }
    }
}