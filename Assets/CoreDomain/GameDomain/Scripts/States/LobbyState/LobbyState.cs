
using System.Threading;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.Scripts.States.LobbyState
{
    public class LobbyState : BaseGameState<LobbyInitiatorEnterData>
    {
        public override GameStateType GameStateType => GameStateType.Lobby;

        public LobbyState(LobbyInitiatorEnterData enterData) : base(enterData)
        {
        }

        public override async Awaitable LoadState(CancellationTokenSource cancellationTokenSource)
        {
            await base.LoadState(cancellationTokenSource);
        }

        public override async Awaitable StartState(CancellationTokenSource cancellationTokenSource)
        {
            await base.StartState(cancellationTokenSource);
        }

        public override async Awaitable ExitState(CancellationTokenSource cancellationTokenSource)
        {
            await base.ExitState(cancellationTokenSource);
        }

        public class Factory : PlaceholderFactory<LobbyInitiatorEnterData, LobbyState>
        {
        }
    }
}
