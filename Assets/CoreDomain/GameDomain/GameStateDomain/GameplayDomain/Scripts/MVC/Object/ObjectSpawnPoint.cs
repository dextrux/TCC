using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObjects;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object
{
    /// <summary>
    /// COLOCAR EM UM GAMEOBJECT VAZIO PARA SPAWNAR O OBJETO EM CENA
    /// </summary>
    public class ObjectSpawnPoint : MonoBehaviour
    {
        [SerializeField] private ObjectConfigurationSo configuration;
        public ObjectConfigurationSo Configuration => configuration;
        
        [Header("Gizmos Configuration")]
        [Tooltip("The color used to display the color of the object")]
        [SerializeField] private Color color = Color.white;
        [SerializeField] private bool showGizmos = true;


        private void OnEnable() => ObjectSpawnPointRegistry.Register(this);

        private void OnDisable() => ObjectSpawnPointRegistry.Unregister(this);

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            if (!configuration.prefab.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null) return;
            
            Gizmos.color = color;
            Gizmos.DrawMesh(meshFilter.sharedMesh, transform.position, transform.rotation);
        }
    }
}