
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using CoreDomain.Scripts.Services.NetworkService;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    /// <summary>
    /// Usar esse script sempre que o Noise + Audio
    /// forem "conversar entre si
    /// </summary>
    public class SoundEmitter : MonoBehaviour, IAudioEmitter, INoiseEmitter
    {
        [Tooltip("Valor de loudness que corresponde a volume 100% no audio. Valores acima disso satura em 1.")]
        [SerializeField] private float maxLoudness = 15f;
        [Tooltip("Ative para sons de objetos cuja emissao deve ser decidida pelo servidor.")]
        [SerializeField] private bool serverOnly;

        private INetworkSoundService _networkSoundService;

        [Inject]
        public void Construct(INetworkSoundService networkSoundService)
        {
            Debug.Log("Constructing SoundEmitter");
            _networkSoundService = networkSoundService;
        }

        // IAudioEmitter - so audio, sem gerar ruido pra IA
        public void EmitAudio(AudioEventId id) => EmitSound(id, 0f);

        // INoiseEmitter - so ruido pra IA, sem tocar audio
        public void EmitNoise(float loudness) => EmitSound(AudioEventId.None, loudness);

        // Toca audio e gera ruido
        public void EmitSound(AudioEventId audioId, float loudness)
        {
            if (_networkSoundService == null)
            {
                Debug.LogError("[SoundEmitter] INetworkSoundService was not injected.");
                return;
            }

            if (float.IsNaN(loudness) || float.IsInfinity(loudness) || loudness < 0f)
            {
                Debug.LogWarning("[SoundEmitter] Invalid loudness.");
                return;
            }

            if (audioId == AudioEventId.None && loudness <= 0f)
            {
                return;
            }

            // Evita emitir um passo quando o AnimationEvent indica parada.
            if (audioId == AudioEventId.Footstep && loudness <= 0f)
            {
                return;
            }

            if (serverOnly)
            {
                _networkSoundService.EmitServerSound((int)audioId, loudness, transform.position, gameObject);
                return;
            }

            _networkSoundService.RequestSound((int)audioId, loudness, transform.position);
        }

        [ContextMenu("Emitir som de teste")]
        public void DebugEmitSound()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("So funciona em Play Mode.");
                return;
            }

            var random = Random.Range(0f, maxLoudness);
            EmitSound(AudioEventId.Test, random * 0.5f);
        }
    }
}
