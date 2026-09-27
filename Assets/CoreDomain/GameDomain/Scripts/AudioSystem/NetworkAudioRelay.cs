using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.Scripts.AudioSystem
{
    public class NetworkAudioRelay : NetworkBehaviour
    {
        private IAudioManager _audioManager;

        [Inject]
        public void Construct(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public void RelaySignal(AudioSignal audioSignal)
        {
            // if (!IsServer)
            // {
            //     Debug.LogError("[NetworkAudioRelay] RelaySignal");
            //     return;
            // }
            Debug.Log("Repotando audio");
            PlayAudioClientRpc(audioSignal.Id, audioSignal.Position, audioSignal.VolumeMultiplier);
        }

        [ClientRpc]
        private void PlayAudioClientRpc(AudioEventId id, Vector3 position, float volumeMultiplier, ClientRpcParams rpcParams = default)
        {
            var signal = new AudioSignal(id, position, null, volumeMultiplier);
            _audioManager.ReportSignal(signal);
        }
    }
}