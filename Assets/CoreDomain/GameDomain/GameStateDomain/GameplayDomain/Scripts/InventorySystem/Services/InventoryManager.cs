using System;
using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;
using Unity.Netcode;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Services
{
    public class InventoryManager : IInventoryManager
    {
        private readonly InventoryConfigurationSo _configurationSo;
        private readonly Dictionary<ulong, PlayerInventory> _playersInventories = new();
        
        public event Action<ulong, PlayerInventory> InventoryChanged;
        
        public InventoryManager(InventoryConfigurationSo configurationSo) => _configurationSo = configurationSo;
        
        public PlayerInventory GetOrCreateInventory(ulong clientId)
        {
            if(_playersInventories.TryGetValue(clientId, out var inventory)) return inventory;

            inventory = new PlayerInventory(_configurationSo.slotCount);
            inventory.OnInventoryChanged += () => InventoryChanged?.Invoke(clientId, inventory);
            _playersInventories.Add(clientId, inventory);
            return inventory;
        }

        public bool TryAddItem(ulong clientId, int itemId, int amount, int maxStacks)
        {
            var networkManager = NetworkManager.Singleton;
            if(networkManager == null || !networkManager.IsServer) return false;
            
            return GetOrCreateInventory(clientId).TryAdd(itemId, amount, maxStacks);
        }

        public bool Remove(ulong clientId)
        {
            return _playersInventories.Remove(clientId);
        }
    }
}