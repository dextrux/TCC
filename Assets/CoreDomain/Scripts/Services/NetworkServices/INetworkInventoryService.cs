using System;

namespace CoreDomain.Scripts.Services.NetworkServices
{
    public interface INetworkInventoryService
    {
        event Action<int> OfflineHeldItemChanged;
        
        void Configure(bool offline, Action<int, int> selectSlot, Action<int, int> scroll, Action<int> removePlayer);

        void SetUp();
        void StopServer();
        void StopClient();
        
        void RemovePlayer(int connectionId);
        
        void RequestSelectSlot(int index);
        void RequestScroll(int direction);
        
        void NotifyHeldItemChanged(int connectionId, int itemId);
    }
}
