using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface
{
    public interface IInventoryActionManager
    {
        // Servidor: arremessa o item selecionado (se for arremessavel) em direcao ao ponto mirado
        bool TryThrowSelected(int connectionId, Vector3 origin, Vector3 aimPoint);
        
        
        // Servidor: tenta utilziar o item selecionado pelo jogador
        bool TryUseSelected(int connectionId, out bool active);
        void OnHeldItemChanged(int connectionId, int heldItemId);
    }
}