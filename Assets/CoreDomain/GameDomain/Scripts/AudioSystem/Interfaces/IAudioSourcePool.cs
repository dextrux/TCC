namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces
{
    public interface IAudioSourcePool
    {
        AudioSourcePooled GetSource();
        void ReleaseSource(AudioSourcePooled source);
        void Prewarm(int count);
    }
}
