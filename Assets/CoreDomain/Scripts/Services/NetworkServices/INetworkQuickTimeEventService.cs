using System;

namespace CoreDomain.Scripts.Services.NetworkServices
{
    public interface INetworkQuickTimeEventService
    {
        event Action<string, int, int> QTEUpdated;

        void Configure(bool offline, Action<int> startAction,
            Action<int, int> inputAction);

        void SetUp();
        void StopServer();
        void StopClient();

        void RequestStart();
        void RequestInput(int key);

        void SendUpdate(int connectionId, string letter, int step, int result);
    }
}