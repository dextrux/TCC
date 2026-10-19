using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.UpdateService;
using Mirror;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Services
{
    public class WorldObjectSpawner : IWorldObjectSpawner
    {
        private readonly ObjectFactory _objectFactory;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly IInventoryManager _inventoryManager;
        private readonly Dictionary<ObjectSpawnPoint, IObjectController> _controllers = new();
        private readonly HashSet<IObjectController> _runtimeControllers = new();

        private bool _isRunning;

        public WorldObjectSpawner(ObjectFactory objectFactory, IUpdateSubscriptionService updateSubscriptionService,
            IInventoryManager inventoryManager)
        {
            _objectFactory = objectFactory;
            _updateSubscriptionService = updateSubscriptionService;
            _inventoryManager = inventoryManager;
        }

        public void StartSpawning()
        {
            if (_isRunning)
            {
                LogService.Log("[WorldObjectSpawner] is already running");
                return;
            }

            if (!NetworkServer.active)
            {
                LogService.Log("[WorldObjectSpawner] Network server is not active");
                return;
            }
            
            _isRunning = true;
            ObjectSpawnPointRegistry.Registered += Spawn;
            ObjectSpawnPointRegistry.Unregistered += Despawn;
            foreach (var point in new List<ObjectSpawnPoint>(ObjectSpawnPointRegistry.All))
            {
                Spawn(point);
            }
        }
        
        private IObjectController CreateController(ObjectConfigurationSo configuration, ObjectView prefab,
            Vector3 position, Quaternion rotation)
        {
            var view = _objectFactory.CreateView(prefab, position, rotation);
            var controller = new ObjectController(configuration, _updateSubscriptionService, view, _inventoryManager);
            
            controller.Setup();
            return controller;
        }
#region SPAWN
        public IObjectController SpawnObject(ObjectConfigurationSo configuration, ObjectView prefab, Vector3 position,
            Quaternion rotation)
        {
            var controller = CreateController(configuration, prefab, position, rotation);
            _runtimeControllers.Add(controller);
            return controller;
        }

        private void Spawn(ObjectSpawnPoint spawnPoint)
        {
            LogService.Log($"[WorldObjectSpawner] Spawning {spawnPoint.Configuration.ObjectName}");
            if (_controllers.ContainsKey(spawnPoint))
            {
                LogService.Log($"[WorldObjectSpawner] Contains {spawnPoint.Configuration.ObjectName}");
                return;
            }

            var view = _objectFactory.CreateView(
                spawnPoint.Configuration.Prefab,
                spawnPoint.transform.position,
                spawnPoint.transform.rotation);
            LogService.Log($"[WorldObjectSpawner] View created {spawnPoint.Configuration.ObjectName}");

            var controller = new ObjectController(
                spawnPoint.Configuration,
                _updateSubscriptionService,
                view,
                _inventoryManager);
            LogService.Log($"[WorldObjectSpawner] Controller created {spawnPoint.Configuration.ObjectName}");
            
            controller.Setup();
            LogService.Log($"[WorldObjectSpawner] Controller setup {spawnPoint.Configuration.ObjectName}");
            _controllers.Add(spawnPoint, controller);
        }
#endregion SPAWN

#region DESPAWN
        private void Despawn(ObjectSpawnPoint spawnPoint)
        {
            if (!_controllers.Remove(spawnPoint, out var controller)) return;
            
            LogService.Log($"[WorldObjectSpawner] Despawning {spawnPoint.Configuration.ObjectName}");
            controller.Dispose();
        }
        
        public void DespawnObject(IObjectController controller)
        {
            if (!_runtimeControllers.Remove(controller)) return;
            
            controller.Dispose();
        }
#endregion DESPAWN

        public void Dispose()
        {
            if (_isRunning)
            {
                _isRunning = false;
                ObjectSpawnPointRegistry.Registered -= Spawn;
                ObjectSpawnPointRegistry.Unregistered -= Despawn;
            }
                    
            foreach(var controller in _controllers.Values) controller.Dispose();
            foreach(var controller in _runtimeControllers) controller.Dispose();
                    
            _controllers.Clear();
            _runtimeControllers.Clear();
        }
    }
}