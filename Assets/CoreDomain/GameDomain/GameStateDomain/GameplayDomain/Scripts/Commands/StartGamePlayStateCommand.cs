using System.Threading;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.AudioService;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.NetworkService;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands
{
    public class StartGamePlayStateCommand : BaseCommand, ICommandAsync
    {
        private ICommandFactory _commandFactory;
        private IAudioService _audioService;
        private INetworkService _networkService;

        private GamePlayInitatorEnterData _enterData;

        public StartGamePlayStateCommand SetEnterData(GamePlayInitatorEnterData enterData)
        {
            _enterData = enterData;
            return this;
        }

        public override void ResolveDependencies()
        {
            _commandFactory = _diContainer.Resolve<ICommandFactory>();
            //_audioService = _diContainer.Resolve<IAudioService>();
            _networkService = _diContainer.Resolve<INetworkService>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource)
        {
            //_gameInputActionsController.EnableInputs();
            await _commandFactory.CreateCommandAsync<StartLevelCommand>().Execute(cancellationTokenSource);

            if (_enterData.SessionMode == NetworkSessionMode.Offline)
            {
                return;
            }

            await _networkService.WaitUntilInitialized(cancellationTokenSource);

            switch (_enterData.SessionMode)
            {
                case NetworkSessionMode.Host:
                {
                    CopyHostProductUserIdToClipboard();
                    _networkService.StartHost();
                    break;
                }

                case NetworkSessionMode.Client:
                {
                    _networkService.StartClient(_enterData.HostProductUserId);
                    break;
                }
            }
        }

        private void CopyHostProductUserIdToClipboard()
        {
            string hostProductUserId = _networkService.LocalProductUserId;

            if (string.IsNullOrWhiteSpace(hostProductUserId))
            {
                LogService.LogWarning("Host Product User ID is empty and could not be copied to clipboard.");
                return;
            }

            GUIUtility.systemCopyBuffer = hostProductUserId;

            LogService.Log("Host Product User ID copied to clipboard: " + hostProductUserId);
        }
    }
}