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

            if (configuration == null || configuration.Prefab == null) return;
            var filter = configuration.Prefab.GetComponentInChildren<MeshFilter>(true);
            if (filter == null || filter.sharedMesh == null) return;
            
            // Respeita o offset e a escala do filho que tem a mesh
            var child = filter.transform;
            Gizmos.DrawMesh(
                filter.sharedMesh,
                transform.TransformPoint(child.localPosition),
                transform.rotation * child.localRotation,
                child.localScale);
        }
        
    }
}