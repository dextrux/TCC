using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem
{
    public class NoiseListener : NetworkBehaviour, INoiseListener
    {
#region Settings Variables
        [Header("Noise Settings")]
        [SerializeField] private float hearingRadius = 10f; 
        [SerializeField, Range(0, 1)] private float minPerceivedIntensity;
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
#endregion INoiseListener Implementation

#region Variables
        private INoiseManager _noiseManager;
        private Vector3? _lastPosition;
        private float _lastPerceivedIntensity;
        private float _alertTimer;
#endregion Variables
        
        [Inject]
        public void Construct(INoiseManager noiseManager) => _noiseManager = noiseManager;
        
        public override void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            _noiseManager.RegisterListener(this);
        }

        public override void OnNetworkDespawn()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            _noiseManager.UnregisterListener(this);
        }
        
        //Trocar o metodo do Update para o novo
        private void Update()
        {
            if (_alertTimer > 0f) 
                _alertTimer -= Time.deltaTime;
        }
        
        public void OnNoiseHeard(NoiseSignal noiseSignal, float perceivedIntensity)
        {
            _lastPosition = noiseSignal.Position;
            _lastPerceivedIntensity = perceivedIntensity;
            _alertTimer = alertDuration;
            
            Debug.Log($"Noise Listener - {name}\nRuido escutado em {_lastPosition} | Intensidade do ruido: {_lastPerceivedIntensity}");

            if (_lastPerceivedIntensity >= 0.9f)
                Debug.Log("BARULHO MUITO ALTO.");
            else if (_lastPerceivedIntensity > 0.6f)
                Debug.Log("BARULHO ALTO.");
            else if (_lastPerceivedIntensity < 0.3f) Debug.Log("BARULHO BAIXO.");
        }
#region ReactionType
        //Criar funcoes para tipos de reacoes diferentes
#endregion ReactionType

        //Nao ta funcionando (?)
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            var isAlert = _alertTimer > 0f;
            Gizmos.color = isAlert ? alertColor : idleColor;
            Gizmos.DrawWireSphere(transform.position, hearingRadius);
            
            var fillColor = Gizmos.color;
            fillColor.a *= 0.3f;
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(transform.position, hearingRadius);

            if (!isAlert || !_lastPosition.HasValue) return;
            
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, _lastPerceivedIntensity);
            Gizmos.DrawLine(transform.position, _lastPosition.Value);
            Gizmos.DrawSphere(_lastPosition.Value, 0.2f + _lastPerceivedIntensity * 0.3f);
        }
    }
}