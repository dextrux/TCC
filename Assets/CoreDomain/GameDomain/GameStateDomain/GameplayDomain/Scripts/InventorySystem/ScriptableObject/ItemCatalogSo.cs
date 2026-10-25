using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject
{
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "InventorySystem/ItemCatalog")]
    public class ItemCatalogSo : UnityEngine.ScriptableObject
    {
        [SerializeField] private ObjectConfigurationSo[] items;
        private Dictionary<int, ObjectConfigurationSo> _byId;

        public bool TryGet(int itemId, out ObjectConfigurationSo inventory)
        {
            if (_byId != null) return _byId.TryGetValue(itemId, out inventory);
            
            _byId = new Dictionary<int, ObjectConfigurationSo>();
            foreach (var item in items)
                if(item != null) 
                    _byId[item.ItemId] = item;

            return _byId.TryGetValue(itemId, out inventory);
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Preenche a lista com os objetos que vao para o inventario (oferenda, inventario e arremessavel)
        /// </summary>
        [ContextMenu("Find All Items")]
        public void FindAllItems()
        {
            var found = new List<ObjectConfigurationSo>();

            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ObjectConfigurationSo"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ObjectConfigurationSo>(path);

                if (asset != null && asset.ObjectType is ObjectType.OfferingObject or ObjectType.InventoryObject or ObjectType.ThrowableObject)
                    found.Add(asset);
            }
            
            items = found.ToArray();
            _byId = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private void OnValidate() => _byId = null;
#endif
    }
}