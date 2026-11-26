using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Hand
{
    /// <summary>
    /// Colocar no prefab do player. So o jogador local (ou o offline) manda input.
    /// </summary>
    public class InventorySelectionInput : MonoBehaviour
    {
        [SerializeField] private PlayerHandView playerHandView;
        [SerializeField] private int hotKeyCount = 3;
        [SerializeField] private KeyCode useKey = KeyCode.E;

        private INetworkInventoryService _networkInventoryService;
        
        [Inject]
        public void Construct(INetworkInventoryService networkInventoryService)
            => _networkInventoryService = networkInventoryService;

        private void Start()
        {
            // if (_networkInventoryService != null) return;
            //
            //
            // LogService.LogError(
            //     $"[InventorySelectionInput] nao injetado | objeto={name} | " +
            //     $"server={Mirror.NetworkServer.active} client={Mirror.NetworkClient.active} " +
            //     $"local={(playerHandView != null && playerHandView.isLocalPlayer)} netId={(playerHandView != null ? playerHandView.netId : 0)}");
        }

        private void Update()
        {
            if (playerHandView == null)
                return;

            if (_networkInventoryService == null)
                return;

            if (NetworkClient.active && !playerHandView.isLocalPlayer)
            {
                return;
            }

            Debug.Log(
                $"[INVENTORY INPUT] Player={playerHandView.name} | " +
                $"Local={playerHandView.isLocalPlayer} | " +
                $"NetId={playerHandView.netId} | " +
                $"Connected={NetworkClient.isConnected}"
            );

            if (Input.GetKeyDown(useKey))
            {
                Debug.Log("[INVENTORY INPUT] Use solicitado");
                _networkInventoryService.RequestUseSelected();
                return;
            }

            for (var i = 0; i < hotKeyCount; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i))
                    continue;

                Debug.Log($"[INVENTORY INPUT] Slot solicitado: {i}");

                _networkInventoryService.RequestSelectSlot(i);
                return;
            }

            var scroll = Input.mouseScrollDelta.y;

            if (scroll > 0f)
            {
                Debug.Log("[INVENTORY INPUT] Scroll anterior");
                _networkInventoryService.RequestScroll(-1);
            }
            else if (scroll < 0f)
            {
                Debug.Log("[INVENTORY INPUT] Scroll próximo");
                _networkInventoryService.RequestScroll(1);
            }
        }
        
        
    }
}
