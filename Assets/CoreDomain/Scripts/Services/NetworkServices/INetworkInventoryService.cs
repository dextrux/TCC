using System;
using UnityEngine;

namespace CoreDomain.Scripts.Services.NetworkServices
{
    public interface INetworkInventoryService
    {
        event Action<int> OfflineHeldItemChanged;
        
        void Configure(bool offline, Action<int, int> selectSlot, Action<int, int> scroll, Action<int> removePlayer, Action<int, Vector3, Vector3> throwAction);

        void SetUp();
        void StopServer();
        void StopClient();
        
        void RemovePlayer(int connectionId);
        
        void RequestSelectSlot(int index);
        void RequestScroll(int direction);
        
        void NotifyHeldItemChanged(int connectionId, int itemId);
        void RequestThrow(Vector3 origin, Vector3 aimPoint);
    }
}
