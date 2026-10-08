using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas
{
    public static class ObjectSpawnPointRegistry
    {
        private static readonly List<ObjectSpawnPoint> Points = new List<ObjectSpawnPoint>();

        public static event Action<ObjectSpawnPoint> Registered;
        public static event Action<ObjectSpawnPoint> Unregistered;

        public static IReadOnlyList<ObjectSpawnPoint> All => Points;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Points.Clear();
            Registered = null;
            Unregistered = null;
        }

        public static void Register(ObjectSpawnPoint point)
        {
            if (Points.Contains(point)) return;

            Points.Add(point);
            Registered?.Invoke(point);
        }

        public static void Unregister(ObjectSpawnPoint point)
        {
            if (!Points.Remove(point)) return;

            Unregistered?.Invoke(point);
        }
    }
}