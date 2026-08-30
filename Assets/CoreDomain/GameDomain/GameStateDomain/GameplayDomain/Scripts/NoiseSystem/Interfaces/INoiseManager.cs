using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces
{
    public interface INoiseManager
    {
        public void RegisterListener(INoiseListener noiseListener);
        public void UnregisterListener(INoiseListener noiseListener);
        public void ReportSignal(NoiseSignal noiseSignal);
    }
}