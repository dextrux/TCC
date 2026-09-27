using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.AudioSystem;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    /// <summary>
    /// Usar esse script sempre que o Noise + Audio
    /// forem "conversar entre si
    /// </summary>
    public class SoundEmitter : NetworkBehaviour, IAudioEmitter, INoiseEmitter
    {
        [Tooltip("Valor de loudness que corresponde a volume 100% no audio. Valores acima disso satura em 1.")]
        [SerializeField] private float maxLoudness = 15f;
        
        private INoiseManager _noiseManager;
        private NetworkAudioRelay _networkAudioRelay;

        [Inject]
        public void Construct(INoiseManager noiseManager, NetworkAudioRelay networkAudioRelay)
        {
            Debug.Log("Constructing SoundEmitter");
            _noiseManager = noiseManager;
            _networkAudioRelay = networkAudioRelay;
        }

        public override void OnNetworkSpawn()
        {
#if UNITY_EDITOR
            Debug.Log($"{name} spawnou na rede. IsSpawned={IsSpawned}, IsOwner={IsOwner}, IsServer={IsServer}");
#endif
        }

        // IAudioEmitter - so audio, sem gerar ruido pra IA
        public void EmitAudio(AudioEventId id) => EmitSound(id, 0f);

        // INoiseEmitter - so ruido pra IA, sem tocar audio
        public void EmitNoise(float loudness) => EmitSound(AudioEventId.None, loudness);

        // Toca audio e gera ruido
        public void EmitSound(AudioEventId audioId, float loudness)
        {
            if (!IsOwner)
            {
#if UNITY_EDITOR
                Debug.Log("Nao foi possivel emitir som pois nao eh o Owner");
#endif
                return;
            }

            EmitSoundServerRpc(audioId, loudness, transform.position);
        }

        [ServerRpc]
        private void EmitSoundServerRpc(AudioEventId audioId, float loudness, Vector3 position)
        {
            if (loudness > 0f)
            {
                var noiseSignal = new NoiseSignal(position, loudness, gameObject);
                _noiseManager.ReportSignal(noiseSignal);
            }

            if (audioId == AudioEventId.None) return;
            
            var volumeMultiplier = loudness > 0f
                ? Mathf.Clamp01(loudness / maxLoudness)
                : 1f;

            var audioSignal = new AudioSignal(audioId, position, gameObject, volumeMultiplier);
            DebugSound.GetVolume(audioId, loudness);
            _networkAudioRelay.RelaySignal(audioSignal);
        }

        [ContextMenu("Emitir som de teste")]
        private void DebugEmitSound()
        {
            if (!Application.isPlaying || !IsSpawned)
            {
                Debug.LogWarning("So funciona em Play Mode com o objeto spawnado.");
                return;
            }

            EmitSound(AudioEventId.Test, maxLoudness * 0.5f);
        }
    }
}