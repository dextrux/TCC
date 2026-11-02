using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;
using CoreDomain.Scripts.Services.Logger.Base;
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
        public int HeldItemId { get; private set; }
        
        
        [SyncVar(hook = nameof(OnHeldActiveChanged))] private bool _heldItemActive;

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
        public void ServerSetHeldItem(int itemId)
        {
            if(itemId != _heldItemId) _heldItemActive = false;
            _heldItemId = itemId;
        }
        [Server]
        public void ServerSetHeldItemActive(bool active) => _heldItemActive = active;   
        
        private void OnHeldItemChanged(int previous, int current) => ApplyVisual(current);
        private void OnHeldActiveChanged(bool previous, bool current) => ApplyActive(current);

        private void ApplyVisual(int itemId)
        {
            HeldItemId = itemId;
            ClearVisual();

            if (itemId == 0 || itemCatalogSo == null || handSocket == null) return;
            if (!itemCatalogSo.TryGet(itemId, out var configuration) || configuration.HeldPrefab == null)
            {
                LogService.LogError($"{nameof(PlayerHandView)}: Could not find configuration for {itemId}");
                return;
            }

            _heldInstance = Instantiate(configuration.HeldPrefab, handSocket);
            _heldInstance.transform.localPosition = Vector3.zero;
            _heldInstance.transform.localRotation = Quaternion.identity;
            
            ApplyActive(netId != 0 && _heldItemActive);
        }
        
        private void ApplyActive(bool active)
        {
            if (_heldInstance == null) return;
            if (_heldInstance.TryGetComponent<IHeldItemActivatable>(out var item)) item.SetActive(active);
        }

        private void ClearVisual()
        {
            if (_heldInstance == null) return;

            Destroy(_heldInstance);
            _heldInstance = null;
        }
    }
}
