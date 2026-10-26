using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface
{
    public interface IInventoryManager
    {
        /// <summary>
        /// Disparado no servidor quando o inventario de alguem muda. O int eh o connectionId do Mirror.
        /// </summary>
        event Action<int, PlayerInventory> InventoryChanged;
        PlayerInventory GetOrCreateInventory(int connectionId);
        bool TryAddItem(int connectionId, int itemId, int amount, int maxStacks);
        bool Remove(int connectionId);
        bool HasItem(int connectionId, int itemId, int amount = 1);
        bool IsHolding(int connectionId, int itemId);
        bool TryTakeRandomOffering(int connectionId, out ItemStack taken);
    }
}
