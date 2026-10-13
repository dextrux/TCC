using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces
{
    public interface INoiseListener
    {
        public void Construct(INoiseManager noiseManager);
        Transform Transform { get; }
        float HearingRadius { get; }
        [Range(0, 1)] float MinPerceivedIntensity { get; }
        void OnNoiseHeard(NoiseSignal noiseSignal, float perceivedIntensity);
    }
}