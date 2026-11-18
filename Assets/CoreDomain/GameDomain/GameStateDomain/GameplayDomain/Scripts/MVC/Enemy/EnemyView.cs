using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy
{
    /// <summary>
    /// Prefab do inimigo: NetworkIdentity + NetworkTransform (ServerToClient) + NavMeshAgent + FieldOfView + NoiseListener.
    /// A IA roda so no servidor; os clientes recebem posicao pelo NetworkTransform e animacao pelo SyncVar.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent),
        typeof(NetworkIdentity))]
    
    [RequireComponent(typeof(FieldOfView),
        typeof(NoiseListener))]
    public class EnemyView : NetworkBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int WalkHash = Animator.StringToHash("Walk");

        [Header("Perception")]
        private FieldOfView _fieldOfView;
        private NoiseListener _noiseListener;

        [Header("NavMesh Settings")]
        [SerializeField] private float navMeshSampleDistance = 2f;

        [SyncVar(hook = nameof(OnAnimStateChanged))] private EnemyAnimState _animState;

        private NavMeshAgent _agent;
        private NavMeshPath _path;

        public NoiseListener NoiseListener => _noiseListener;
        public bool IsMoving => _agent.enabled && _agent.velocity.sqrMagnitude > 0.05f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _fieldOfView = GetComponent<FieldOfView>();
            _noiseListener = GetComponent<NoiseListener>();
            _path = new NavMeshPath();

            // So o servidor usa o agent. Nos clientes o NetworkTransform move o objeto.
            _agent.enabled = false;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (NavMesh.SamplePosition(transform.position, out var hit, navMeshSampleDistance, NavMesh.AllAreas))
                transform.position = hit.position;
            else
                Debug.LogWarning($"[EnemyView] {name} nasceu longe da NavMesh.");

            _agent.enabled = true;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyAnimation(_animState);
        }

#region Movement (servidor)
        public void SetSpeed(float speed)
        {
            if (_agent.enabled) _agent.speed = speed;
        }

        public bool SetDestination(Vector3 point)
        {
            if (!_agent.enabled || !_agent.isOnNavMesh) return false;
            if (!NavMesh.SamplePosition(point, out var hit, navMeshSampleDistance, NavMesh.AllAreas)) return false;

            _agent.isStopped = false;
            return _agent.SetDestination(hit.position);
        }

        public void StopMoving()
        {
            if (!_agent.enabled || !_agent.isOnNavMesh) return;

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public bool HasReachedDestination()
        {
            if (!_agent.enabled || !_agent.isOnNavMesh) return false;
            if (_agent.pathPending) return false;

            // Caminho invalido: considera "chegou" para o inimigo nao ficar preso
            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;

            return _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
        }

        /// <summary>
        /// Distancia a pe ate o ponto, pela NavMesh. Infinito se nao ha caminho completo.
        /// </summary>
        public float GetPathDistance(Vector3 point)
        {
            if (!_agent.enabled || !_agent.isOnNavMesh) return float.PositiveInfinity;
            if (!NavMesh.SamplePosition(point, out var hit, navMeshSampleDistance, NavMesh.AllAreas))
                return float.PositiveInfinity;
            if (!_agent.CalculatePath(hit.position, _path) || _path.status != NavMeshPathStatus.PathComplete)
                return float.PositiveInfinity;

            var corners = _path.corners;
            var distance = 0f;
            for (var i = 1; i < corners.Length; i++)
                distance += Vector3.Distance(corners[i - 1], corners[i]);

            return distance;
        }
#endregion Movement (servidor)

#region Perception (servidor)
        public bool TryGetVisibleTarget(out Transform target)
        {
            target = null;
            return _fieldOfView != null && _fieldOfView.TryGetVisibleTarget(out target);
        }
#endregion Perception (servidor)

#region Animation
        public void SetAnimation(EnemyAnimState state)
        {
            if (!isServer || _animState == state) return;

            _animState = state;
            // No host/servidor dedicado o hook nao garante rodar: aplica direto tambem
            ApplyAnimation(state);
        }

        private void OnAnimStateChanged(EnemyAnimState previous, EnemyAnimState current) => ApplyAnimation(current);

        private void ApplyAnimation(EnemyAnimState state)
        {
            if (animator == null) return;

            var walking = state == EnemyAnimState.Walk;
            animator.SetBool(WalkHash, walking);
            animator.SetBool(IdleHash, !walking);
        }
#endregion Animation
    }
}
