using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.ScriptableObject;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private EnemyConfigurationSo configuration;
        public EnemyConfigurationSo Configuration => configuration;

        [Header("Patrol")]
        [Tooltip("Rota de patrulha em ordem. O inimigo volta ao primeiro depois do ultimo")]
        [SerializeField] private Transform[] waypoints;
        
        [Header("Debug Gizmos")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color color = Color.white;

        private void OnEnable() => EnemySpawnPointRegistry.RegisterEnemy(this);
        private void OnDisable() => EnemySpawnPointRegistry.UnregisterEnemy(this);

        public Vector3[] GetWaypointPositions()
        {
            if (waypoints == null) return System.Array.Empty<Vector3>();

            var positions = new List<Vector3>(waypoints.Length);
            foreach (var waypoint in waypoints)
                if (waypoint != null) positions.Add(waypoint.position);

            return positions.ToArray();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = color;

            if (configuration != null && configuration.EnemyPrefab != null)
            {
                var filter = configuration.EnemyPrefab.GetComponentInChildren<MeshFilter>(true);
                if (filter != null && filter.sharedMesh != null)
                {
                    // Respeita o offset e a escala do filho que tem a mesh
                    var child = filter.transform;
                    Gizmos.DrawMesh(
                        filter.sharedMesh,
                        transform.TransformPoint(child.localPosition),
                        transform.rotation * child.localRotation,
                        child.localScale);
                }
            }

            if (waypoints == null || waypoints.Length == 0) return;

            // Do ponto de spawn ate o primeiro waypoint (por onde a patrulha comeca)
            if (waypoints[0] != null)
                Gizmos.DrawLine(transform.position, waypoints[0].position);

            for (var i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

                var next = waypoints[(i + 1) % waypoints.Length];
                if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
            }
        }
    }
}