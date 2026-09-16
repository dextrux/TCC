using System.Threading;
using CoreDomain.GameDomain.Scripts.States.LobbyState;
using CoreDomain.Scripts.Services.CommandFactory;
using CoreDomain.Scripts.Utils;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.LobbyDomain.Scripts.Commands.EntryPoint
{
    public class EnterLobbyStateCommand: BaseCommand, ICommandAsync
    {

        public EnterLobbyStateCommand SetEnterData()
        {
            return this;
        }
        
        public override void ResolveDependencies()
        {
        }

        public Awaitable Execute(CancellationTokenSource cancellationTokenSource)
        {
            return AwaitableUtils.CompletedTask;
        }
    }
}
