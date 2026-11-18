using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using CoreDomain.Scripts.Services.Logger.Base;
using Mirror;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Services
{
    public class InventoryActionManager : IInventoryActionManager
    {
        private const float MaxOriginDistance = 3f;

        private readonly IInventoryManager _inventoryManager;
        private readonly IWorldObjectSpawner _worldObjectSpawner;
        private readonly ItemCatalogSo _itemCatalog;
        
        private readonly Dictionary<int, int> _activeHeldItems = new();

        public InventoryActionManager(IInventoryManager inventoryManager, IWorldObjectSpawner worldObjectSpawner,
            ItemCatalogSo itemCatalog)
        {
            _inventoryManager = inventoryManager;
            _worldObjectSpawner = worldObjectSpawner;
            _itemCatalog = itemCatalog;
        }

        public bool TryThrowSelected(int connectionId, Vector3 origin, Vector3 aimPoint)
        {
            if (!NetworkServer.active) return false;

            if (!IsNearPlayer(connectionId, origin))
            {
                LogService.Log($"[InventoryActionService] Origem do arremesso longe demais do player {connectionId}.");
                return false;
            }

            var inventory = _inventoryManager.GetOrCreateInventory(connectionId);
            var stack = inventory.SelectedStack;
            if (stack.IsEmpty()) return false;

            if (!_itemCatalog.TryGet(stack.ItemId, out var config) || config.ObjectType != ObjectType.ThrowableObject)
                return false;

            var direction = ThrowAim.GetLaunchDirection(origin, aimPoint, config.ThrowForce, config.Gravity);

            if (!inventory.TryRemoveSelected(1, out _)) return false;

            var controller = _worldObjectSpawner.SpawnObject(config, config.Prefab, origin, Quaternion.LookRotation(direction));
            controller.Throw(origin, direction);
            return true;
        }

        public bool TryUseSelected(int connectionId, out bool active)
        {
            active = false;
            
            var inventory = _inventoryManager.GetOrCreateInventory(connectionId);
            var stack = inventory.SelectedStack;
            if(stack.IsEmpty()) return false;
            
            if(!_itemCatalog.TryGet(stack.ItemId, out var config) || config.ObjectType != ObjectType.InventoryObject)
                return false;

            if (!config.CanBeActivated) return false;

            if (_activeHeldItems.TryGetValue(connectionId, out var activeId) && activeId == stack.ItemId)
            {
                _activeHeldItems.Remove(connectionId);
                return true;
            }

            var required = config.RequiredItemToActivate;
            if (required != null && !inventory.HasItem(required.ItemId))
            {
                LogService.Log($"[InventoryActionService] Item {required.ItemId} not found.");
                return false;
            }
            
            inventory.RemoveItem(required.ItemId);
            _activeHeldItems[connectionId] = stack.ItemId;
            active = true;
            return true;
        }

        public void OnHeldItemChanged(int connectionId, int heldItemId)
        {
            if(_activeHeldItems.TryGetValue(connectionId, out var activeId) && activeId != heldItemId)
                _activeHeldItems.Remove(connectionId); 
        }

        private static bool IsNearPlayer(int connectionId, Vector3 origin)
        {
            if (!NetworkServer.connections.TryGetValue(connectionId, out var connection) || connection.identity == null)
                return true;

            return (origin - connection.identity.transform.position).sqrMagnitude
                   <= MaxOriginDistance * MaxOriginDistance;
        }
    }
}
