using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface
{
    public interface IWorldObjectSpawner : IDisposable
    {
        void StartSpawning();
        IObjectController SpawnObject(ObjectConfigurationSo configuration, ObjectView prefab, Vector3 position, Quaternion rotation);
        void DespawnObject(IObjectController controller);
    }
}