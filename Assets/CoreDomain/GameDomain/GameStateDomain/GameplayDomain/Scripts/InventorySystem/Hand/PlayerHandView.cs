using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Hand
{
    /// <summary>
    /// Colocar no prefab do player
    /// </summary>
    public class PlayerHandView : NetworkBehaviour
    {
        [SerializeField] private Transform handSocket;
        [SerializeField] private ItemCatalogSo itemCatalogSo;
        
        private readonly NetworkVariable<int> _heldItemId = new NetworkVariable<int>();
        
        private IInventoryManager _inventoryManager;
        private GameObject _heldInstance;
        
        [Inject]
        public void Construct(IInventoryManager inventoryManager) => _inventoryManager = inventoryManager;
        
        public override void OnNetworkSpawn()
        {
            Debug.Log($"[PlayerHandView] OnNetworkSpawn | IsServer={IsServer} IsOwner={IsOwner}");
            base.OnNetworkSpawn();
            
            _heldItemId.OnValueChanged += OnHeldItemChanged;
            ApplyVisual(_heldItemId.Value);
            
            if (!IsServer)
            {
                Debug.Log($"[PlayerHandView]: Nao eh o servidor");
                return;
            }
            
            if(_inventoryManager == null)
            {
                Debug.LogError($"[PlayerHandView]: inventoryManager nao foi injetado");
                return;
            }
            
            Debug.Log($"[PlayerHandView]: Instanciado");
            _inventoryManager.InventoryChanged += OnInventoryChanged;
            Refresh(_inventoryManager.GetOrCreateInventory(OwnerClientId));
        }

        public override void OnNetworkDespawn()
        {
            _heldItemId.OnValueChanged -= OnHeldItemChanged;

            if (IsServer && _inventoryManager != null)
                _inventoryManager.InventoryChanged -= OnInventoryChanged;

            ClearVisual();
            Debug.Log($"[PlayerHandView]: Despawn");
            base.OnNetworkDespawn();
        }

        [ServerRpc]
        public void SelectSlotServerRpc(int index)
        {
            _inventoryManager?.GetOrCreateInventory(OwnerClientId).Selected(index);
        }

        [ServerRpc]
        public void ScrollServerRpc(int direction)
        {
            _inventoryManager?.GetOrCreateInventory(OwnerClientId).ScrollToSelected(Mathf.Clamp(direction, -1, 1));
        }

        private void OnInventoryChanged(ulong clientId, PlayerInventory inventory)
        {
            if (clientId != OwnerClientId) return;
            
            Refresh(inventory);
        }

        private void Refresh(PlayerInventory inventory)
        {
            var stack = inventory.SelectedStack;
            _heldItemId.Value = stack.IsEmpty() ? 0 : stack.ItemId;
        }

        private void OnHeldItemChanged(int previous, int current) => ApplyVisual(current);

        private void ApplyVisual(int itemId)
        {
            ClearVisual();

            if (itemId == 0 || itemCatalogSo == null || handSocket == null) return;
            if (!itemCatalogSo.TryGet(itemId, out var configuration) || configuration.heldPrefab == null) return;
            
            _heldInstance = Instantiate(configuration.heldPrefab, handSocket);
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