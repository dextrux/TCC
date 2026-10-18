using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Hand
{
    public class PlayerHandView : NetworkBehaviour, INetworkHeldItemTarget
    {
        [SerializeField] private Transform handSocket;
        [SerializeField] private ItemCatalogSo itemCatalogSo;
        
        [SyncVar(hook = nameof(OnHeldItemChanged))]
        private int _heldItemId;

        private INetworkInventoryService _networkInventoryService;
        private GameObject _heldInstance;

        // Usado so no offline (player criado pelo Zenject). Online esta nulo e nao eh necessario.
        [Inject]
        public void Construct(INetworkInventoryService networkInventoryService)
            => _networkInventoryService = networkInventoryService;

        private void Start()
        {
            if (_networkInventoryService != null)
                _networkInventoryService.OfflineHeldItemChanged += ApplyVisual;
        }

        private void OnDestroy()
        {
            if (_networkInventoryService != null)
                _networkInventoryService.OfflineHeldItemChanged -= ApplyVisual;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisual(_heldItemId);
        }

        public override void OnStopClient()
        {
            ClearVisual();
            base.OnStopClient();
        }

        /// <summary>
        /// Chamado pelo servico no servidor.
        /// </summary>
        [Server]
        public void ServerSetHeldItem(int itemId) => _heldItemId = itemId;

        private void OnHeldItemChanged(int previous, int current) => ApplyVisual(current);

        private void ApplyVisual(int itemId)
        {
            ClearVisual();

            if (itemId == 0 || itemCatalogSo == null || handSocket == null) return;
            if (!itemCatalogSo.TryGet(itemId, out var configuration) || configuration.HeldPrefab == null) return;

            _heldInstance = Instantiate(configuration.HeldPrefab, handSocket);
            _heldInstance.transform.localPosition = Vector3.zero;
            _heldInstance.transform.localRotation = Quaternion.identity;
        }

        private void ClearVisual()
        {
            if (_heldInstance == null) return;

            Destroy(_heldInstance);
            _heldInstance = null;
        }
    }
}
