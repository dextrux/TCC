using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObjects;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface
{
    public interface IWorldObjectSpawner : IDisposable
    {
        /// <summary>
        /// Da spawn em objetos ja cadastrados e tambem nos que se cadastrarem depois
        /// </summary>
        void StartSpawning();
        
        /// <summary>
        ///    Cria um objeto sem spawn point. SO NO SERVIDOR
        /// </summary>
        IObjectController SpawnObject(ObjectConfigurationSo configuration, ObjectView prefab, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Remove um objeto criado por SpawnObject
        /// </summary>>
        void DespawnObject(IObjectController controller);
    }
}