
using System;
using UnityEngine;

namespace CoreDomain.Scripts.Services.NetworkService
{
    public interface INetworkSoundService
    {
        void Configure(bool offline, Action<int, Vector3, float> playAudio, Action<Vector3, float, GameObject> reportNoise);
        void SetUp();
        void StopServer();
        void StopClient();
        void RequestSound(int soundId, float loudness, Vector3 position);
        void EmitServerSound(int soundId, float loudness, Vector3 position, GameObject source = null);
    }
}
