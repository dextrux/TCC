using CoreDomain.Scripts.Services.Logger.Base;
using Mirror;
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
            if (!NetworkServer.active)
                throw new System.InvalidOperationException(
                        "[ObjectFactory] CreateView so pode ser chamado pelo servidor");
            
            LogService.Log($"[ObjectFactory] CreateView {prefab.name}");
            var view = UnityEngine.Object.Instantiate(prefab, position, rotation);
            _container.InjectGameObject(view.gameObject);
            
            if(view.netIdentity.netId == 0)
                NetworkServer.Spawn(view.gameObject);

            return view;
        }
    }
}