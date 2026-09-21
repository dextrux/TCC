using System.Threading;
using UnityEngine;

namespace CoreDomain.Scripts.Services.NetworkService
{
    public interface INetworkService
    {
        string LocalProductUserId { get; }
        bool IsInitialized { get; }

        Awaitable WaitUntilInitialized(CancellationTokenSource cancellationTokenSource);
        void StartHost();
        void StartClient(string hostProductUserId);
        void Disconnect();
    }
}