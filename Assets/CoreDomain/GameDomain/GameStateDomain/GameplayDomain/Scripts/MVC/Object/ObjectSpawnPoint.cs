using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object
{
    public class ObjectSpawnPoint : MonoBehaviour
    {
        [SerializeField] private ObjectConfigurationSo configuration;
        public ObjectConfigurationSo Configuration => configuration;
        
        [Header("Debug Gizmos")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color color = Color.white;

        private void OnEnable() => ObjectSpawnPointRegistry.RegisterObject(this);
        private void OnDisable() => ObjectSpawnPointRegistry.UnregisterObject(this);

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            if (!configuration.Prefab.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) return;
            
            Gizmos.color = color;
            Gizmos.DrawMesh(filter.sharedMesh, transform.position, transform.rotation);
        }
    }
}