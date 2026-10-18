using System;
using System.Collections.Generic;
using CoreDomain.Scripts.Services.Logger.Base;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas
{
    public class ObjectSpawnPointRegistry
    {
        public static readonly List<ObjectSpawnPoint> Points = new();
        
        public static event Action<ObjectSpawnPoint> Registered;
        public static event Action<ObjectSpawnPoint> Unregistered;

        public static IReadOnlyList<ObjectSpawnPoint> All => Points;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetState()
        {
            Points.Clear();
            Registered = null;
            Unregistered = null;
        }

        public static void RegisterObject(ObjectSpawnPoint point)
        {
            if (Points.Contains(point)) return;
            
            LogService.Log($"Registering point {point.Configuration.ObjectName}");
            Points.Add(point);
            Registered?.Invoke(point);
        }

        public static void UnregisterObject(ObjectSpawnPoint point)
        {
            if (!Points.Contains(point)) return;
            
            LogService.Log($"Unregistering point {point.Configuration.ObjectName}");
            Points.Remove(point);
            Unregistered?.Invoke(point);
        }
    }
}