using System;
using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Services
{
    public class InventoryManager : IInventoryManager
    {
        private readonly InventoryConfigurationSo _configurationSo;
        private readonly Dictionary<int, PlayerInventory> _playersInventories = new();

        public event Action<int, PlayerInventory> InventoryChanged;

        public InventoryManager(InventoryConfigurationSo configurationSo) => _configurationSo = configurationSo;

        public PlayerInventory GetOrCreateInventory(int connectionId)
        {
            if (_playersInventories.TryGetValue(connectionId, out var inventory)) return inventory;

            inventory = new PlayerInventory(_configurationSo.slotCount);
            inventory.OnInventoryChanged += () => InventoryChanged?.Invoke(connectionId, inventory);
            _playersInventories.Add(connectionId, inventory);
            return inventory;
        }

        public bool TryAddItem(int connectionId, int itemId, int amount, int maxStacks)
            => GetOrCreateInventory(connectionId).TryAdd(itemId, amount, maxStacks);

        public bool Remove(int connectionId) => _playersInventories.Remove(connectionId);
    }
}
