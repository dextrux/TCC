
using System.Threading;
using UnityEngine;

namespace CoreDomain.Scripts.Services.NetworkService
{
    public interface INetworkService
    {
        string LocalProductUserId { get; }
        bool IsInitialized { get; }
        bool SupportsLocalKcp { get; }
        ushort LocalKcpPort { get; }

        Awaitable WaitUntilInitialized(CancellationTokenSource cancellationTokenSource);

        void StartHost();
        void StartLocalHost();
        void StartClient(string address);
        void Disconnect();
        void NotifyGameplayReady();
    }
}
