using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas
{
    public struct AudioSignal
    {
        public AudioEventId Id;
        public Vector3 Position;
        public GameObject Source;

        public AudioSignal(AudioEventId id, Vector3 position, GameObject source)
        {
            Id = id;
            Position = position;
            Source = source;
        }
    }
}
