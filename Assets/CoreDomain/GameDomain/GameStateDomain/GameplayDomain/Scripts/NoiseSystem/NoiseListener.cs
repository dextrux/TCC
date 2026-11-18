
using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem
{
    public class NoiseListener : MonoBehaviour, ICompetingNoiseListener
    {
#region Settings Variables
        [Header("Noise Settings")]
        [SerializeField] private float hearingRadius = 10f;
        [SerializeField, Range(0, 1)] private float minPerceivedIntensity;
        [Tooltip("Se varios ouvintes que competem ouvirem o mesmo ruido, so o mais proximo reage (inimigos)")]
        [SerializeField] private bool competeForNoise;
#endregion Settings Variables

#region Debug Variables
        [Header("Debug")]
        [SerializeField] private bool showGizmos;
        [SerializeField] private Color idleColor = new Color(0f, 1f, 1f, 0.25f);
        [SerializeField] private Color alertColor = new Color(1f, 0.2f, 0.2f, 0.4f);
        [SerializeField] private float alertDuration = 1.5f;
#endregion Debug Variables

#region INoiseListener Implementation
        public Transform Transform => transform;
        public float HearingRadius => hearingRadius;
        public float MinPerceivedIntensity => minPerceivedIntensity;
        public bool CompetesForNoise => competeForNoise;

        // Disparado quando ESTE ouvinte deve reagir ao ruido
        public event Action<NoiseSignal, float> NoiseHeard;

        private Func<Vector3, float> _responseCostProvider;

        // O dono (ex.: inimigo) pode trocar o custo padrao (distancia reta) por distancia na NavMesh
        public void SetResponseCostProvider(Func<Vector3, float> provider) => _responseCostProvider = provider;

        public float GetResponseCost(Vector3 noisePosition)
            => _responseCostProvider?.Invoke(noisePosition) ?? Vector3.Distance(transform.position, noisePosition);
#endregion INoiseListener Implementation

#region Variables
        private INoiseManager _noiseManager;
        private Vector3? _lastPosition;
        private float _lastPerceivedIntensity;
        private float _alertTimer;
#endregion Variables

        public void Construct(INoiseManager noiseManager)
        {
            if (_noiseManager != null && _noiseManager != noiseManager)
            {
                _noiseManager.UnregisterListener(this);
            }

            _noiseManager = noiseManager;

            if (!isActiveAndEnabled || _noiseManager == null) return;
            
            _noiseManager.RegisterListener(this);
        }

        public void Release()
        {
            if (_noiseManager == null) return;
            
            _noiseManager.UnregisterListener(this);
            _noiseManager = null;
        }

        private void OnEnable()
        {
            _noiseManager?.RegisterListener(this);
        }

        private void OnDisable()
        {
            _noiseManager?.UnregisterListener(this);
        }

        private void OnDestroy()
        {
            Release();
        }

        //Trocar o metodo do Update para o novo
        private void Update()
        {
            if (_alertTimer > 0f)
            {
                _alertTimer -= Time.deltaTime;
            }
        }

        public void OnNoiseHeard(NoiseSignal noiseSignal, float perceivedIntensity)
        {
            _lastPosition = noiseSignal.Position;
            _lastPerceivedIntensity = perceivedIntensity;
            _alertTimer = alertDuration;

            Debug.Log($"Noise Listener - {name}\nRuido escutado em {_lastPosition} | Intensidade do ruido: {_lastPerceivedIntensity}");

            switch (_lastPerceivedIntensity)
            {
                case >= 0.9f:
                    Debug.Log("BARULHO MUITO ALTO.");
                    break;
                case >= 0.4f:
                    Debug.Log("BARULHO ALTO.");
                    break;
                case < 0.4f:
                    Debug.Log("BARULHO BAIXO.");
                    break;
            }

            NoiseHeard?.Invoke(noiseSignal, perceivedIntensity);
        }

#region ReactionType
        //Criar funcoes para tipos de reacoes diferentes
#endregion ReactionType

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            var isAlert = _alertTimer > 0f;
            Gizmos.color = isAlert ? alertColor : idleColor;
            Gizmos.DrawWireSphere(transform.position, hearingRadius);

            var fillColor = Gizmos.color;
            fillColor.a *= 0.3f;
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(transform.position, hearingRadius);

            if (!isAlert || !_lastPosition.HasValue)
            {
                return;
            }

            Gizmos.color = Color.Lerp(Color.yellow, Color.red, _lastPerceivedIntensity);
            Gizmos.DrawLine(transform.position, _lastPosition.Value);
            Gizmos.DrawSphere(_lastPosition.Value, 0.2f + _lastPerceivedIntensity * 0.3f);
        }
    }
}
