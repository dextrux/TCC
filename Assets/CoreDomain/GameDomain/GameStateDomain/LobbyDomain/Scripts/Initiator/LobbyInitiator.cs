using System.Threading;
using CoreDomain.GameDomain.GameStateDomain.LobbyDomain.Scripts.Commands.EntryPoint;
using CoreDomain.GameDomain.Scripts.States.LobbyState;
using CoreDomain.Scripts.CoreInitiator.Base;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.InitiatorInvokerService;
using CoreDomain.Scripts.Services.SceneService;
using CoreDomain.Scripts.Utils;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.LobbyDomain.Scripts.Initiator {
    public class LobbyInitiator : ISceneInitiator, ILobbyInitiator {
        private readonly ICommandFactory _commandFactory;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;
        public ScenesType SceneType => ScenesType.LobbyScene;


        public LobbyInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService) {
            _sceneInitiatorsService = sceneInitiatorsService;
            _commandFactory = commandFactory;
            _sceneInitiatorsService.RegisterInitiator(this);
        }

        public async Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            var enterData = (LobbyInitiatorEnterData)enterDataObject;
            await _commandFactory.CreateCommandAsync<EnterLobbyStateCommand>().SetEnterData().Execute(cancellationTokenSource);
        }

        public Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            return AwaitableUtils.CompletedTask;
        }

        public Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource) {
            _sceneInitiatorsService.UnregisterInitiator(this);
            _commandFactory.CreateCommandVoid<ExitLobbyStateCommand>().Execute();
            return AwaitableUtils.CompletedTask;
        }
    }
}