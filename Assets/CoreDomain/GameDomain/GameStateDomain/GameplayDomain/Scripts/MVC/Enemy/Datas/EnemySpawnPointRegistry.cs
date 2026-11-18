using System;
using System.Collections.Generic;
using CoreDomain.Scripts.Services.Logger.Base;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Datas
{
    public class EnemySpawnPointRegistry
    {
        public static readonly List<EnemySpawnPoint> Points = new();
        
        public static event Action<EnemySpawnPoint> Registered;
        public static event Action<EnemySpawnPoint> Unregistered;

        public static IReadOnlyList<EnemySpawnPoint> All => Points;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetState()
        {
            Points.Clear();
            Registered = null;
            Unregistered = null;
        }

        public static void RegisterEnemy(EnemySpawnPoint point)
        {
            if (Points.Contains(point)) return;
            
            //LogService.Log($"Registering point {point.Configuration.EnemyName}");
            Points.Add(point);
            Registered?.Invoke(point);
        }

        public static void UnregisterEnemy(EnemySpawnPoint point)
        {
            if (!Points.Contains(point)) return;
            
            //LogService.Log($"Unregistering point {point.Configuration.EnemyName}");
            Points.Remove(point);
            Unregistered?.Invoke(point);
        }
    }
}