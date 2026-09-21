using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.Scripts.AudioSystem
{
    /// <summary>
    /// Fica no mesmo NetworkObject do servidor.
    /// O AudioService.ReportSignal, ao rodar no servidor, deve chamar aqui
    /// para replicar o som a todos os clientes.
    /// </summary>
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
            if (!IsServer) return;
            PlayAudioClientRpc(audioSignal.Id, audioSignal.Position);
        }

        [ClientRpc]
        private void PlayAudioClientRpc(AudioEventId id, Vector3 position, ClientRpcParams rpcParams = default)
        {
            var signal = new AudioSignal(id, position, null);
            _audioManager.ReportSignal(signal);
        }
    }
}
