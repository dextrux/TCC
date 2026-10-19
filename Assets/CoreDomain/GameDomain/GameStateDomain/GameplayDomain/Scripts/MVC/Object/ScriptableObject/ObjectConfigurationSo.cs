using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject
{
    public enum ObjectType
    {
        InteractObject,
        OfferingObject,
        ThrowableObject,
        InventoryObject
    }
    
    [CreateAssetMenu(fileName = "SO_Object_", menuName = "ObjectConfiguration/New Object", order = 0)]
    public class ObjectConfigurationSo : UnityEngine.ScriptableObject
    {
        [Header("Object Configuration")]
        [SerializeField] private string objectName;
        [SerializeField] private int itemId;
        [SerializeField] private ObjectType objectType = ObjectType.InteractObject;
        [SerializeField, Min(1)] private int maxStackSize = 1;

        [Header("Throw Configuration")] 
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float throwForce = 10f;
        [SerializeField] private float throwHeight = 1.6f;
        
        [Header("Interaction Configuration")]
        [SerializeField] private string activateAction;
        [SerializeField] private string deactivateAction;

        [Header("Visual Configuration")] 
        [SerializeField] private ObjectView prefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject heldPrefab;
        
        public string ObjectName => objectName;
        public int ItemId => itemId;
        public ObjectType ObjectType => objectType;
        public int MaxStackSize => maxStackSize;
        public float Gravity => gravity;
        public float ThrowForce => throwForce;
        public float ThrowHeight => throwHeight;
        public string ActivateAction => activateAction;
        public string DeactivateAction => deactivateAction;
        public ObjectView Prefab => prefab;
        public Sprite Icon => icon;
        public GameObject HeldPrefab => heldPrefab;
        
#if UNITY_EDITOR
        private void OnValidate() => GenerateId();

        private void GenerateId()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path)) return;
            
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            var generator = int.Parse(guid[..8], System.Globalization.NumberStyles.HexNumber);
            
            if(generator == 0) generator = 1;
            if (itemId == generator) return;
            
            itemId = generator;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}