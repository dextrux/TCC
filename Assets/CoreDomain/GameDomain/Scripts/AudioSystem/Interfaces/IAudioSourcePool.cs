namespace CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces
{
    public interface IAudioSourcePool
    {
        AudioSourcePooled GetSource();
        void ReleaseSource(AudioSourcePooled source);
        void Prewarm(int count);
    }
}
