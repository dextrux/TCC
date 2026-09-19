using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas
{
    public struct NoiseSignal
    {
        public Vector3 Position;
        public float Loudness;
        public GameObject Source;

        public NoiseSignal(Vector3 position, float loudness, GameObject source)
        {
            Position = position;
            Loudness = loudness;
            Source = source;
        }
    }
}