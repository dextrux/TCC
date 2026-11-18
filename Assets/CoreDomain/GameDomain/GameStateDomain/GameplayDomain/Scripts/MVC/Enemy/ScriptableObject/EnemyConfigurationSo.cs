using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.ScriptableObject
{
    [CreateAssetMenu(fileName = "SO_Enemy_", menuName = "EnemyConfiguration/New Enemy", order = 1)]
    public class EnemyConfigurationSo : UnityEngine.ScriptableObject
    {
        [SerializeField] private string enemyName;
        [SerializeField] private int enemyId;
        
        [SerializeField] private EnemyView enemyPrefab;

        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float investigateSpeed = 3.5f;
        [SerializeField] private float chaseSpeed = 5f;

        [Header("Patrol")]
        [Tooltip("Tempo parado em cada waypoint")]
        [SerializeField] private float waypointWaitTime = 2f;

        [Header("Perception")]
        [Tooltip("De quanto em quanto tempo o campo de visao eh checado")]
        [SerializeField] private float visionScanInterval = 0.2f;
        [Tooltip("Tempo sem ver o jogador ate desistir da perseguicao")]
        [SerializeField] private float loseTargetTime = 3f;
        [Tooltip("Tempo procurando no local do ruido (ou onde viu o jogador pela ultima vez)")]
        [SerializeField] private float investigateWaitTime = 3f;

        public string EnemyName => enemyName;
        public int EnemyId => enemyId;
        public EnemyView EnemyPrefab => enemyPrefab;

        public float PatrolSpeed => patrolSpeed;
        public float InvestigateSpeed => investigateSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float WaypointWaitTime => waypointWaitTime;
        public float VisionScanInterval => Mathf.Max(0.05f, visionScanInterval);
        public float LoseTargetTime => loseTargetTime;
        public float InvestigateWaitTime => investigateWaitTime;
        
#if UNITY_EDITOR
        private void OnValidate() => GenerateId();

        private void GenerateId()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path)) return;
            
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            var generator = int.Parse(guid[..8], System.Globalization.NumberStyles.HexNumber);
            
            if(generator == 0) generator = 1;
            if (enemyId == generator) return;
            
            enemyId = generator;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}