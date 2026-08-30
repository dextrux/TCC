using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Services
{
    [System.Serializable]
    public class NoiseManager : INoiseManager
    {
        private readonly List<INoiseListener> _noiseListeners = new();
        private readonly NoiseDecaySettings _noiseDecaySettings;
        
        //Buffer para evitar alocacao de GC
        private readonly RaycastHit[] _hit = new RaycastHit[16];

        public NoiseManager(NoiseDecaySettings noiseDecaySettings)
        {
            _noiseDecaySettings = noiseDecaySettings;
        }
        
        public void RegisterListener(INoiseListener noiseListener)
        {
            if(!_noiseListeners.Contains(noiseListener))
                _noiseListeners.Add(noiseListener);
        }

        public void UnregisterListener(INoiseListener noiseListener)
        {
            if(_noiseListeners.Contains(noiseListener))
                _noiseListeners.Remove(noiseListener);
        }

        public void ReportSignal(NoiseSignal noiseSignal)
        {
            Debug.Log($"Sinal reportado do game object {noiseSignal.Source} na posicao {noiseSignal.Position}");
            foreach (var noiseListener in _noiseListeners)
            {
                var distance = Vector3.Distance(noiseListener.Transform.position, noiseSignal.Position);
                var maxPossibleRange = Mathf.Max(noiseSignal.Loudness, noiseListener.HearingRadius);
                
                if(distance > maxPossibleRange || maxPossibleRange <= 0)
                {
                    Debug.Log($"Distancia {distance} eh maior que o range maximo {maxPossibleRange}");
                    continue;
                }
                
                var obstacleMultiplier = CalculateObstacleAttenuation(
                    noiseSignal.Position, 
                    noiseListener.Transform.position, 
                    distance);

                if (obstacleMultiplier <= 0) continue;
                
                var normalizedDistance = Mathf.Clamp01(distance / maxPossibleRange);
                var curve = _noiseDecaySettings.curve.Evaluate(normalizedDistance);
                var perceivedIntensity = Mathf.Clamp01(curve * obstacleMultiplier);

#if UNITY_EDITOR
                Debug.Log(perceivedIntensity);
#endif

                if (perceivedIntensity >= noiseListener.MinPerceivedIntensity)
                {
                    noiseListener.OnNoiseHeard(noiseSignal, perceivedIntensity);
                    Debug.Log($"Sinal reportado para o {noiseListener.Transform.gameObject.name}.");
                }
            }
        }

        private float CalculateObstacleAttenuation(Vector3 origin, Vector3 target, float distance)
        {
            if (distance < 0.001f) return 1f;

            var direction = (target - origin) / distance;

            var hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                _hit,
                distance,
                _noiseDecaySettings.obstacleMask
            );
            
            #if UNITY_EDITOR
                Debug.DrawLine(origin, target, hitCount > 0 ? Color.yellow : Color.green, 1f);
            #endif

            if (hitCount <= 0f)
            {
                #if UNITY_EDITOR
                    Debug.Log("Nenhum obstaculo na frente.");
                #endif
                return 1f;
            }

            if (hitCount > _noiseDecaySettings.maxObstaclesPenetrated)
            {
                #if UNITY_EDITOR
                    Debug.Log($"Muitos obstaculos na frente.\nBarulho não emitido para o inimigo");
                #endif
                return 0f;
            }

            var multiplier = 1f;
            for (var i = 0; i < hitCount; i++)
            {
                multiplier *= _noiseDecaySettings.attenuationPerObstacle;
            }

            return multiplier;
        }
    }
}