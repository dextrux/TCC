using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces
{
    public interface IAudioEmitter
    {
        void EmitAudio(AudioEventId id);
    }
}
