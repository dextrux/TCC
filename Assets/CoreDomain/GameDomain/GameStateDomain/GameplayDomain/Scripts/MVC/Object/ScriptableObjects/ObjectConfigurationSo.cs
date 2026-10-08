using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObjects
{
    public enum ObjectType
    {
        InteractObject, // Portas, baus, alavancas etc
        OfferingObject, // Oferendas
        ThrowableObject, // Garrafas, pedras etc - OBJETOS QUE SERAO JOGADOS
    }

    [CreateAssetMenu(fileName = "ObjectConfiguration", menuName = "Object/Object Configuration", order = 0)]
    public class ObjectConfigurationSo : ScriptableObject
    {
        [Header("Object")] public string objectName;
        //TODO: Ainda precisa ser feito as interacoes com cada tipo
        public ObjectType objectType = ObjectType.InteractObject; 

        [Header("Throw")]
        public float gravity = -9.81f;

        [Tooltip("Velocidade inicial (m/s) ao ser arremessado")]
        public float objectSpeedWhenThrown = 10f;
        
        [Header("Interact")]
        public string activateAction = "open";
        public string deactivateAction = "close";
    }
}