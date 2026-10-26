using System;
using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Services
{
    public class InventoryManager : IInventoryManager
    {
        private readonly InventoryConfigurationSo _configurationSo;
        private readonly ItemCatalogSo _itemCatalog;
        private readonly Dictionary<int, PlayerInventory> _playersInventories = new();

        public event Action<int, PlayerInventory> InventoryChanged;

        public InventoryManager(InventoryConfigurationSo configurationSo, ItemCatalogSo itemCatalog)
        {
            _configurationSo = configurationSo;
            _itemCatalog = itemCatalog;
        }

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

        public bool HasItem(int connectionId, int itemId, int amount = 1)
            => GetOrCreateInventory(connectionId).HasItem(itemId, amount);

        public bool IsHolding(int connectionId, int itemId)
            => GetOrCreateInventory(connectionId).IsHolding(itemId);

        //TODO: USAR ESSA FUNCAO PARA RETIRAR OFERENDA ALEATORIA DO JOGADOR
        //TODO: COMO UTILIZAR: _inventoryManager.TryTakeRandomOffering(idDoJogador, out var taken);
        public bool TryTakeRandomOffering(int connectionId, out ItemStack taken)
            => GetOrCreateInventory(connectionId).TryTakeRandom(IsOffering, out taken);

        private bool IsOffering(int itemId)
            => _itemCatalog.TryGet(itemId, out var item) && item.ObjectType == ObjectType.OfferingObject;
    }
}
