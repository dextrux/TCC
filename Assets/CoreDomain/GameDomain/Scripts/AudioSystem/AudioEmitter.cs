using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem
{
    public class AudioEmitter : NetworkBehaviour, IAudioEmitter
    {
        [SerializeField] private AudioEventId defaultEvent;

        private IAudioManager _audioManager;
        private NetworkAudioRelay _networkAudioRelay;

        [Inject]
        public void Construct(IAudioManager audioManager, NetworkAudioRelay networkAudioRelay)
        {
            _audioManager = audioManager;
            _networkAudioRelay = networkAudioRelay;
        }

        public override void OnNetworkSpawn()
        {
            SetupToZenject();
        }
        
        private void SetupToZenject()
        {
            var sceneContext = FindAnyObjectByType(typeof(SceneContext)) as SceneContext;
            if 
                (sceneContext != null) sceneContext.Container.Inject(this);
            else 
                Debug.LogError("[AudioEmitter] SceneContext nao encontrado para injecao manual.");

#if UNITY_EDITOR
            Debug.Log($"{name} spawnou na rede. IsSpawned={IsSpawned}, IsOwner={IsOwner}, IsServer={IsServer}");
#endif
        }
        
        public void EmitAudio(AudioEventId id)
        {
            if (!IsOwner)
            {
#if UNITY_EDITOR
                Debug.Log("Nao foi possivel emitir audio pois nao eh o Owner");
#endif
                return;
            }
            
#if UNITY_EDITOR
            Debug.Log($"{name} | Enviado o audio: {id} para o ServerRpc");
#endif
            EmitAudioServerRpc(id, transform.position);
        }

        [ServerRpc]
        private void EmitAudioServerRpc(AudioEventId id, Vector3 position)
        {
            Debug.Log($"{name} | ServerRpc recebeu o audio: {id}");
            
            var signal = new AudioSignal(id, position, gameObject);
            _networkAudioRelay.RelaySignal(signal);
        }

        [ContextMenu("Emitir audio de teste")]
        private void DebugEmitAudio()
        {
            if (!Application.isPlaying || !IsSpawned)
            {
#if UNITY_EDITOR
                Debug.LogWarning("So funciona em Play Mode com o objeto spawnado.");
#endif
                return;
            }
            
#if UNITY_EDITOR
            Debug.Log($"{name} | Testando envio de audio");
#endif
            EmitAudio(defaultEvent);
        }

#region AnimationEvents
        public void EmitFootstepOnAnimation() => EmitAudio(AudioEventId.FootstepConcrete);
        public void EmitDoorOpenOnAnimation() => EmitAudio(AudioEventId.DoorOpen);
        public void EmitDoorCloseOnAnimation() => EmitAudio(AudioEventId.DoorClose);
#endregion AnimationEvents
    }
}
