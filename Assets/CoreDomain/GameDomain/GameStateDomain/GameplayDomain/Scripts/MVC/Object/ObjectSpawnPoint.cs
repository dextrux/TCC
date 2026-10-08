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
        [SerializeField] private ObjectView prefab;
        
        [Header("Gizmos Configuration")]
        [Tooltip("The color used to display the color of the object")]
        [SerializeField] private Color color = Color.white;
        [SerializeField] private bool showGizmos = true;

        public ObjectConfigurationSo Configuration => configuration;
        public ObjectView Prefab => prefab;

        private void OnEnable() => ObjectSpawnPointRegistry.Register(this);

        private void OnDisable() => ObjectSpawnPointRegistry.Unregister(this);

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            var mesh = prefab?.GetComponent<MeshFilter>().sharedMesh;
            
            if (mesh is null) return;
            
            Gizmos.color = color;
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation);
        }
    }
}