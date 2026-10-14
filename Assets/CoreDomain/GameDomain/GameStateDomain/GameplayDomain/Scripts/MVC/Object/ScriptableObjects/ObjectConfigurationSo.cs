using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObjects
{
    public enum ObjectType
    {
        InteractObject, // Portas, baus, alavancas etc
        OfferingObject, // Oferendas
        ThrowableObject, // Garrafas, pedras etc - OBJETOS QUE SERAO JOGADOS
        InventoryObject //Tochas, pederneira etc - Objetos que vao ficar no inventario do player
    }

    [CreateAssetMenu(fileName = "ObjectConfiguration", menuName = "Object/Object Configuration", order = 0)]
    public class ObjectConfigurationSo : ScriptableObject
    {
        [Header("Object")] public string objectName;
        [SerializeField] private int itemId;
        //TODO: Ainda precisa ser feito as interacoes com cada tipo
        public ObjectType objectType = ObjectType.InteractObject;
        [Tooltip("Quantos itens deste tipo cabem em um unico slot do inventario")]
        [Min(1)] public int maxStackSize = 1;

        [Header("Throw")]
        public float gravity = -9.81f;

        [Tooltip("Velocidade inicial (m/s) ao ser arremessado")]
        public float objectSpeedWhenThrown = 10f;
        public float throwOriginHeight = 1.6f;
        
        [Header("Interact")]
        public string activateAction = "open";
        public string deactivateAction = "close";


        [Header("Visual")]
        public ObjectView prefab;
        public Sprite icon;
        [Tooltip("Apenas o visual que vai aparecer na mao do jogador - Nao pode possuir rb, collider ou networkobject")]
        public GameObject heldPrefab;
        
        public int ItemId => itemId;
        
#if UNITY_EDITOR
        private void OnValidate() => GenerateId();

        private void GenerateId()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if(string.IsNullOrEmpty(path)) return;
            
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            var generator = int.Parse(guid[..8], System.Globalization.NumberStyles.HexNumber);

            if(generator == 0) generator = 1;
            
            if(itemId == generator) return;
            
            itemId = generator;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}