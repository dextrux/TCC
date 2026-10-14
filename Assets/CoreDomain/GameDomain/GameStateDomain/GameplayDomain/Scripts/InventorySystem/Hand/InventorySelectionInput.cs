using CoreDomain.Scripts.Services.UpdateService;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Hand
{
    /// <summary>
    /// Colocar no prefab do player
    /// </summary>
    public class InventorySelectionInput : MonoBehaviour, IUpdatable
    {
        [SerializeField] private PlayerHandView playerHandView;
        [SerializeField] private int hotKeyCount = 3;

        public void Update()
        {
            OnScrollUsed();
        }

        public void ManagedUpdate()
        {
            throw new System.NotImplementedException();
        }
        
        private void OnScrollUsed()
        {
            if (playerHandView == null || !playerHandView.IsSpawned || !playerHandView.IsOwner)
            {
                Debug.Log($"{playerHandView} | {playerHandView.IsSpawned} | {playerHandView.IsOwner}");
                return;
            }
            
            for (var i = 0; i < hotKeyCount; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
                
                playerHandView.SelectSlotServerRpc(i);
                return;
            }
            
            var scroll = Input.mouseScrollDelta.y;
            switch (scroll)
            {
                case > 0:
                    Debug.Log($"[PlayerInventory] Scrolling up");
                    playerHandView.ScrollServerRpc(-1); //scroll pra cima = slot anterior
                    break;
                case < 0:
                    Debug.Log($"[PlayerInventory] Scrolling down");
                    playerHandView.ScrollServerRpc(1); //scroll pra baixo = proximo slot
                    break;
            }
        }

    }
}