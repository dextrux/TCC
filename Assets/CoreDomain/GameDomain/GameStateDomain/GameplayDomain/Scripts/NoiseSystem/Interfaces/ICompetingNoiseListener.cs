using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces
{
    /// <summary>
    /// Ouvinte que COMPETE pelo ruido: entre todos que competem e ouviram o mesmo ruido,
    /// so o de menor custo (o mais proximo) recebe o OnNoiseHeard.
    /// </summary>
    public interface ICompetingNoiseListener : INoiseListener
    {
        bool CompetesForNoise { get; }

        // Menor = mais proximo. float.PositiveInfinity = nao consegue chegar ate o ruido.
        float GetResponseCost(Vector3 noisePosition);
    }
}
