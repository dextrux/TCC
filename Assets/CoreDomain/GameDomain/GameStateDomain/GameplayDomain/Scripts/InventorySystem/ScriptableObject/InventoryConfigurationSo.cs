using System;
using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using UnityEngine;
using UnityEngine.Serialization;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject
{
    [CreateAssetMenu(fileName = "InventoryConfiguration", menuName = "InventorySystem/InventoryConfiguration")]
    public class InventoryConfigurationSo : UnityEngine.ScriptableObject
    {
        [Min(1)] public int slotCount = 3;
        
        [SerializeField] private List<StartingItem> startingItems = new();
        public IReadOnlyList<StartingItem> StartingItems => startingItems;
    }
    
    [Serializable]
    public class StartingItem
    {
        public ObjectConfigurationSo item;
        [Min(1)] public int amount = 1;
    }
}