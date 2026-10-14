using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObjects;
using CoreDomain.Scripts.Services.UpdateService;
using Unity.Netcode;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Services
{
    public class WorldObjectSpawner : IWorldObjectSpawner
    {
        private readonly ObjectFactory _factory;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly IInventoryManager _inventoryManager;
        private readonly Dictionary<ObjectSpawnPoint, IObjectController> _controllers = new();
        private readonly HashSet<IObjectController> _runtimeControllers = new();

        private bool _isRunning;
        
        public WorldObjectSpawner(ObjectFactory factory, IUpdateSubscriptionService updateSubscriptionService, IInventoryManager inventoryManager)
        {
            _factory = factory;
            _updateSubscriptionService = updateSubscriptionService;
            _inventoryManager = inventoryManager;
        }
        
        public void StartSpawning()
        {
            if (_isRunning) { Debug.Log("[Spawner] já estava rodando"); return; }

            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer) return;

            _isRunning = true;
            ObjectSpawnPointRegistry.Registered += Spawn;
            ObjectSpawnPointRegistry.Unregistered += Despawn;
            foreach (var point in new List<ObjectSpawnPoint>(ObjectSpawnPointRegistry.All))
                Spawn(point);
        }

        public IObjectController SpawnObject(ObjectConfigurationSo configuration, ObjectView prefab, 
            Vector3 position, Quaternion rotation)
        {
            var controller = CreateController(configuration, prefab, position, rotation);
            _runtimeControllers.Add(controller);
            return controller;
        }

        public void DespawnObject(IObjectController controller)
        {
            if (!_runtimeControllers.Remove(controller)) return;

            controller.Dispose();
        }

        private void Spawn(ObjectSpawnPoint spawnPoint)
        {
            Debug.Log($"[WorldObjectSpawner] Spawning {spawnPoint.Configuration.prefab}");
            if (_controllers.ContainsKey(spawnPoint)) return;
            
            var view = _factory.CreateView(
                spawnPoint.Configuration.prefab,
                spawnPoint.transform.position,
                spawnPoint.transform.rotation);

            var controller = new ObjectController(
                spawnPoint.Configuration,
                _updateSubscriptionService,
                _inventoryManager,
                view);
            
            controller.Setup();
            
            _controllers.Add(spawnPoint, controller);
        }

        private void Despawn(ObjectSpawnPoint spawnPoint)
        {
            if (!_controllers.Remove(spawnPoint, out var controller)) return;
            
            Debug.Log($"[WorldObjectSpawner] Despawning {spawnPoint.Configuration.prefab}");
            controller.Dispose();
        }
        
        private IObjectController CreateController(ObjectConfigurationSo configuration, ObjectView prefab, 
            Vector3 position, Quaternion rotation)
        {
            var view = _factory.CreateView(prefab, position, rotation);

            var controller = new ObjectController(configuration, _updateSubscriptionService, _inventoryManager, view);
            controller.Setup();
            
            return controller;
        }
        
        public void Dispose()
        {
            if (_isRunning)
            {
                _isRunning = false;
                ObjectSpawnPointRegistry.Registered -= Spawn;
                ObjectSpawnPointRegistry.Unregistered -= Despawn;
            }
            
            foreach (var controller in _controllers.Values) controller.Dispose();
            foreach (var controller in _runtimeControllers) controller.Dispose();
            
            _controllers.Clear();
            _runtimeControllers.Clear();
        }

    }
}