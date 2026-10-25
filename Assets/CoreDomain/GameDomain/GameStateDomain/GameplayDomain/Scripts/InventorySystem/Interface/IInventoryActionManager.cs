using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface
{
    public interface IInventoryActionManager
    {
        // Servidor: arremessa o item selecionado (se for arremessavel) em direcao ao ponto mirado
        bool TryThrowSelected(int connectionId, Vector3 origin, Vector3 aimPoint);
    }
}