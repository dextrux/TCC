using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas
{
    [System.Serializable]
    public class NoiseDecaySettings
    {
        public LayerMask obstacleMask;
        
        //Reduz tanto do ruido por obstaculo
        [Range(0, 1)]public float attenuationPerObstacle;
        public int maxObstaclesPenetrated;
        
        //Curva que calcula a intensidade e distancia do barulho - X = Distância | Y = Intensidade
        public AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 0);
    }
}