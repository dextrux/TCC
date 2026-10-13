using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    [System.Serializable]
    public struct SoundEntry
    {
        public AudioEventId id;
        public float loudness;
    }
}