using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.UpdateService;
using Mirror;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Services
{
    public class WorldEnemySpawner : IWorldEnemySpawner
    {
        private readonly EnemyFactory _enemyFactory;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly INoiseManager _noiseManager;
        private readonly Dictionary<EnemySpawnPoint, IEnemyController> _controllers = new();

        private bool _isRunning;

        public WorldEnemySpawner(EnemyFactory enemyFactory, IUpdateSubscriptionService updateSubscriptionService,
            INoiseManager noiseManager)
        {
            _enemyFactory = enemyFactory;
            _updateSubscriptionService = updateSubscriptionService;
            _noiseManager = noiseManager;
        }

        public void StartSpawning()
        {
            if (_isRunning)
            {
                LogService.Log("[WorldEnemySpawner] is already running");
                return;
            }

            if (!NetworkServer.active)
            {
                LogService.Log("[WorldEnemySpawner] Network server is not active");
                return;
            }

            _isRunning = true;
            EnemySpawnPointRegistry.Registered += Spawn;
            EnemySpawnPointRegistry.Unregistered += Despawn;

            foreach (var point in new List<EnemySpawnPoint>(EnemySpawnPointRegistry.All))
                Spawn(point);
        }

        private void Spawn(EnemySpawnPoint spawnPoint)
        {
            if (_controllers.ContainsKey(spawnPoint)) return;

            var view = _enemyFactory.CreateView(
                spawnPoint.Configuration.EnemyPrefab,
                spawnPoint.transform.position,
                spawnPoint.transform.rotation);

            var controller = new EnemyController(
                spawnPoint.Configuration,
                view,
                spawnPoint.GetWaypointPositions(),
                _updateSubscriptionService,
                _noiseManager);

            controller.Setup();
            _controllers.Add(spawnPoint, controller);
        }

        private void Despawn(EnemySpawnPoint spawnPoint)
        {
            if (!_controllers.Remove(spawnPoint, out var controller)) return;

            controller.Dispose();
        }

        public void Dispose()
        {
            if (_isRunning)
            {
                _isRunning = false;
                EnemySpawnPointRegistry.Registered -= Spawn;
                EnemySpawnPointRegistry.Unregistered -= Despawn;
            }

            foreach (var controller in _controllers.Values) controller.Dispose();
            _controllers.Clear();
        }
    }
}
