using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Services
{
    public class ObjectFactory
    {
        private readonly DiContainer _container;
        
        public ObjectFactory(DiContainer container) => _container = container;
        
        public ObjectView CreateView(ObjectView prefab, Vector3 position, Quaternion rotation)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer)
                throw new System.InvalidOperationException("ObjectFactory.CreateView so pode ser chamado pelo servidor.");
            
            Debug.Log($"Creating object {prefab.name} view ");
            var view = UnityEngine.Object.Instantiate(prefab, position, rotation);
            _container.InjectGameObject(view.gameObject);
            
            if(!view.NetworkObject.IsSpawned)
                view.NetworkObject.Spawn();

            return view;
        }
    }
}