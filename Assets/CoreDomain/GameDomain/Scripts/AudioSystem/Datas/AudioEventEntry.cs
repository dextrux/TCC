using System;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Datas
{
    [Serializable]
    public struct AudioEventEntry
    {
        public AudioEventId id;
        public AudioEvent audioEvent;
    }
}