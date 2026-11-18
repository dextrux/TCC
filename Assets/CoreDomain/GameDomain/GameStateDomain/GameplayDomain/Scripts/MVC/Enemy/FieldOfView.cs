using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy
{
    /// <summary>
    /// Campo de visao do inimigo. Nao roda sozinho: o EnemyController chama TryGetVisibleTarget
    /// em intervalos (so no servidor). Retorna o alvo visivel mais proximo.
    /// O cone eh HORIZONTAL (angle) e cobre da altura dos olhos ate o chao: um alvo agachado ou
    /// rente ao inimigo continua sendo visto.
    /// </summary>
    public class FieldOfView : MonoBehaviour
    {
        public float radius = 10f;
        [Range(0, 360)]
        public float angle = 90f;

        public LayerMask targetMask;
        public LayerMask obstructionMask;

        [Tooltip("So a POSICAO de onde o olhar sai (altura dos olhos). A direcao vem sempre do transform deste componente")]
        [SerializeField] private Transform eyes;

        [Tooltip("Quanto acima dos olhos ainda eh visto. Para baixo a visao vai ate o chao")]
        [SerializeField] private float maxHeightAboveEyes = 0.5f;

        // Somente para debug (Inspector e FieldOfViewEditor)
        public bool canSeePlayer;
        public Transform VisibleTarget { get; private set; }

        private const float SampleMargin = 0.1f;

        private readonly Collider[] _buffer = new Collider[16];

        public Vector3 EyePosition => eyes != null ? eyes.position : transform.position;

        public Vector3 EyeForward => transform.forward;

        public float FloorY => transform.position.y;

        public bool TryGetVisibleTarget(out Transform target)
        {
            target = null;

            var origin = EyePosition;
            var forwardFlat = Vector3.ProjectOnPlane(EyeForward, Vector3.up);
            forwardFlat = forwardFlat.sqrMagnitude > 0.0001f ? forwardFlat.normalized : Vector3.forward;

            var closest = float.PositiveInfinity;

            var count = Physics.OverlapSphereNonAlloc(origin, radius, _buffer, targetMask);
            for (var i = 0; i < count; i++)
            {
                var targetCollider = _buffer[i];
                var bounds = targetCollider.bounds;

                // Muito acima dos olhos nao eh visto
                if (bounds.min.y > origin.y + maxHeightAboveEyes) continue;

                var toCenter = bounds.center - origin;

                // Angulo so no plano horizontal: alvo embaixo do olho (ate o chao) continua dentro do cone
                var flat = Vector3.ProjectOnPlane(toCenter, Vector3.up);
                if (flat.sqrMagnitude > 0.0001f && Vector3.Angle(forwardFlat, flat) > angle * 0.5f) continue;

                if (!HasLineOfSight(origin, bounds)) continue;

                var distance = toCenter.magnitude;
                if (distance >= closest) continue;

                closest = distance;
                target = targetCollider.transform;
            }

            VisibleTarget = target;
            canSeePlayer = target != null;
            return canSeePlayer;
        }

        // Visivel se qualquer ponto (cabeca, centro ou pes) nao tiver obstaculo no caminho
        private bool HasLineOfSight(Vector3 origin, Bounds bounds)
        {
            var center = bounds.center;
            var head = new Vector3(center.x, Mathf.Max(center.y, bounds.max.y - SampleMargin), center.z);
            var feet = new Vector3(center.x, Mathf.Min(center.y, bounds.min.y + SampleMargin), center.z);

            return IsClear(origin, center) || IsClear(origin, head) || IsClear(origin, feet);
        }

        private bool IsClear(Vector3 origin, Vector3 point)
        {
            var toPoint = point - origin;
            var distance = toPoint.magnitude;
            if (distance <= Mathf.Epsilon) return true;

            return !Physics.Raycast(origin, toPoint / distance, distance, obstructionMask);
        }
    }
}