using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces
{
    public interface IAudioEmitter
    {
        void EmitAudio(AudioEventId id);
    }
}
