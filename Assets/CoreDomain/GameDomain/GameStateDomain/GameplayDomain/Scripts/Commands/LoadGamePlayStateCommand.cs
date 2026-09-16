using System.Threading;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.AudioService;
using CoreDomain.Scripts.Services.CommandFactory;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.EntryPoint {
    public class LoadGamePlayStateCommand : BaseCommand, ICommandAsync {
        private IAudioService _audioService;
        private ICommandFactory _commandFactory;

        private GamePlayInitatorEnterData _enterData;

        public LoadGamePlayStateCommand SetEnterData(GamePlayInitatorEnterData enterData) {
            _enterData = enterData;
            return this;
        }

        public override void ResolveDependencies() {
            //_audioService = _diContainer.Resolve<IAudioService>();
            _commandFactory = _diContainer.Resolve<ICommandFactory>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            await _commandFactory.CreateCommandAsync<LoadLevelCommand>().SetEnterData(new LoadLevelCommandData(_enterData.LevelNumberToEnter)).Execute(cancellationTokenSource);
        }
    }
}