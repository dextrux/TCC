using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas
{
    [System.Serializable]
    public class NoiseDecaySettings
    {
        public LayerMask obstacleMask;
        [Range(0, 1)]public float attenuationPerObstacle;
        public int maxObstaclesPenetrated;
        
        [Tooltip("X = Distância | Y = Intensidade")] //Curva que calcula a intensidade e distancia do barulho
        public AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 0);
    }
}