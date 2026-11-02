using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using UnityEngine;

public class TorchSystem : MonoBehaviour, IHeldItemActivatable
{
    [SerializeField] private Light torchLight;
    [SerializeField] private GameObject flame;

    public bool IsLit { get; private set; }

    public void SetActive(bool lit)
    {
        IsLit = lit;

        if (torchLight != null) torchLight.enabled = lit;
        if (flame == null) return;

        flame.SetActive(lit);
    }
}