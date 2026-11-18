using Mirror;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Services
{
    public class EnemyFactory
    {
        private readonly DiContainer _container;

        public EnemyFactory(DiContainer container) => _container = container;

        public EnemyView CreateView(EnemyView prefab, Vector3 position, Quaternion rotation)
        {
            if (!NetworkServer.active)
                throw new System.InvalidOperationException("EnemyFactory.CreateView so pode ser chamado pelo servidor.");

            var view = UnityEngine.Object.Instantiate(prefab, position, rotation);
            _container.InjectGameObject(view.gameObject);

            // O prefab precisa estar em "Registered Spawnable Prefabs" do NetworkManager
            if (view.netIdentity.netId == 0)
                NetworkServer.Spawn(view.gameObject);

            return view;
        }
    }
}
