using UnityEngine;
using UnityEngine.Serialization;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject
{
    [CreateAssetMenu(fileName = "InventoryConfiguration", menuName = "InventorySystem/InventoryConfiguration")]
    public class InventoryConfigurationSo : UnityEngine.ScriptableObject
    {
        [Min(1)] public int slotCount = 3;
    }
}