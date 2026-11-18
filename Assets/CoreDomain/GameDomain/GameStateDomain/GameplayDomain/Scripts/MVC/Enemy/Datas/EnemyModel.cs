using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Datas
{
    public enum EnemyState
    {
        Patrol,
        Investigate,
        Chase
    }

    // Estado de animacao sincronizado com os clientes
    public enum EnemyAnimState : byte
    {
        Idle,
        Walk
    }

    /// <summary>
    /// Dados e regras simples do inimigo. Nao conhece NavMesh, rede nem GameObjects.
    /// </summary>
    public class EnemyModel
    {
        private float _waitTimer;

        public EnemyState State { get; private set; } = EnemyState.Patrol;
        public int WaypointIndex { get; private set; }

        public Vector3 LastKnownTargetPosition { get; private set; }
        public float LoseTargetTimer { get; private set; }
        public bool IsTargetVisible => LoseTargetTimer <= 0f;

        public bool IsWaiting { get; private set; }

        public void SetState(EnemyState state)
        {
            State = state;
            IsWaiting = false;
            _waitTimer = 0f;
        }

        public void SeeTarget(Vector3 position)
        {
            LastKnownTargetPosition = position;
            LoseTargetTimer = 0f;
        }

        public void AddLoseTime(float deltaTime) => LoseTargetTimer += deltaTime;

        public void StartWaiting(float duration)
        {
            IsWaiting = true;
            _waitTimer = Mathf.Max(0f, duration);
        }

        /// <summary>
        /// Retorna true no tick em que a espera termina.
        /// </summary>
        public bool TickWait(float deltaTime)
        {
            if (!IsWaiting) return false;

            _waitTimer -= deltaTime;
            if (_waitTimer > 0f) return false;

            IsWaiting = false;
            return true;
        }

        public int NextWaypoint(int count)
        {
            if (count <= 0) return 0;

            WaypointIndex = (WaypointIndex + 1) % count;
            return WaypointIndex;
        }
    }
}
