
using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using CoreDomain.GameDomain.Scripts.Inputs;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.CommandFactory;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands {
    public class StartLevelCommand : BaseCommand, ICommandAsync {
        private LoadLevelCommandData _commandData;
        private IInputManager _inputManager;
        private ILevelCancellationTokenService _levelCancellationTokenService;
        private ILevelScenarioController _levelScenarioController;

        private GamePlayInitatorEnterData _enterData;

        public StartLevelCommand SetEnterData(GamePlayInitatorEnterData enterData) {
            _enterData = enterData;
            return this;
        }

        public override void ResolveDependencies() {
            _inputManager = _diContainer.Resolve<IInputManager>();
            _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
            _levelScenarioController = _diContainer.Resolve<ILevelScenarioController>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            _inputManager.EnablePLayerInput();
            //Inserir await para iniciar a fase com algum comando
            //Os NoiseListeners serao registrados pelo GamePlayInitiator apos iniciar a sessao.
        }
    }
}
