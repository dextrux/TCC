using UnityEngine;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Datas
{
    public struct AudioSignal
    {
        public AudioEventId Id;
        public Vector3 Position;
        public GameObject Source;
        public float VolumeMultiplier;

        public AudioSignal(AudioEventId id, Vector3 position, GameObject source, float volumeMultiplier = 1f)
        {
            Id = id;
            Position = position;
            Source = source;
            VolumeMultiplier = volumeMultiplier;
        }
    }
}
