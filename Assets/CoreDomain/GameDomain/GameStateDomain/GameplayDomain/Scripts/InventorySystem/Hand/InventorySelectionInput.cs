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

        private INetworkInventoryService _networkInventoryService;
        
        [Inject]
        public void Construct(INetworkInventoryService networkInventoryService)
            => _networkInventoryService = networkInventoryService;

        private void Start()
        {
            if (_networkInventoryService != null) return;

            //TODO: CASO O PLAYER DO NGO ESTIVER SPAWNANDO, RETIRE-O E O ERRO DEIXARA DE EXISTIR!
            LogService.LogError(
                $"[InventorySelectionInput] nao injetado | objeto={name} | " +
                $"server={Mirror.NetworkServer.active} client={Mirror.NetworkClient.active} " +
                $"local={(playerHandView != null && playerHandView.isLocalPlayer)} netId={(playerHandView != null ? playerHandView.netId : 0)}");
        }

        private void Update()
        {
            return;
            if (_networkInventoryService == null || playerHandView == null) return;

            // Offline: nao ha NetworkClient. Online: so o player local.
            var canControl = !NetworkClient.active || playerHandView.isLocalPlayer;
            if (!canControl) return;

            for (var i = 0; i < hotKeyCount; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

                _networkInventoryService.RequestSelectSlot(i);
                return;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (scroll > 0f) _networkInventoryService.RequestScroll(-1); // scroll pra cima = slot anterior
            else if (scroll < 0f) _networkInventoryService.RequestScroll(1); // scroll pra baixo = proximo slot
        }
    }
}
