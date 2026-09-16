using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.EntryPoint;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.CoreInitiator.Base;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Services.InitiatorInvokerService;
using CoreDomain.Scripts.Services.SceneService;
using CoreDomain.Scripts.Utils;
using System.Threading;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator {

    public class GamePlayInitiator : ISceneInitiator, IGamePlayInitiator {
        private readonly ICommandFactory _commandFactory;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;

        public ScenesType SceneType => ScenesType.GamePlayScene;

        public GamePlayInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService) {
            _commandFactory = commandFactory;
            _sceneInitiatorsService = sceneInitiatorsService;
            _sceneInitiatorsService.RegisterInitiator(this);
        }

        public async Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            GamePlayInitatorEnterData enterData = (GamePlayInitatorEnterData)enterDataObject;
            await _commandFactory.CreateCommandAsync<LoadGamePlayStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);
        }

        public async Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            var enterData = (GamePlayInitatorEnterData)enterDataObject;
            await _commandFactory.CreateCommandAsync<StartGamePlayStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);
        }

        public async Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource) {
            _sceneInitiatorsService.UnregisterInitiator(this);
            _commandFactory.CreateCommandVoid<ExitGamePlayStateCommand>().Execute();
            await AwaitableUtils.CompletedTask;
        }
    }
}