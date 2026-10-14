using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface
{
    public interface IInventoryManager
    {
        event Action<ulong, PlayerInventory> InventoryChanged;
        PlayerInventory GetOrCreateInventory(ulong clientId);
        bool TryAddItem(ulong clientId, int itemId, int amount, int maxStacks);
        bool Remove(ulong clientId);
    }
}