
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
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

        private INoiseManager _noiseManager;
        private IAudioManager _audioManager;

        [Inject]
        public void Construct(INoiseManager noiseManager, IAudioManager audioManager)
        {
            Debug.Log("Constructing SoundEmitter");
            _noiseManager = noiseManager;
            _audioManager = audioManager;
        }

        // IAudioEmitter - so audio, sem gerar ruido pra IA
        public void EmitAudio(AudioEventId id) => EmitSound(id, 0f);

        // INoiseEmitter - so ruido pra IA, sem tocar audio
        public void EmitNoise(float loudness) => EmitSound(AudioEventId.None, loudness);

        // Toca audio e gera ruido
        public void EmitSound(AudioEventId audioId, float loudness)
        {
            if (_noiseManager == null || _audioManager == null)
            {
                Debug.LogError("[SoundEmitter] Dependencies were not injected.");
                return;
            }

            if (float.IsNaN(loudness) || float.IsInfinity(loudness) || loudness < 0f)
            {
                Debug.LogWarning("[SoundEmitter] Invalid loudness value.");
                return;
            }

            if (loudness > 0f)
            {
                var noiseSignal = new NoiseSignal(transform.position, loudness, gameObject);
                _noiseManager.ReportSignal(noiseSignal);
            }

            if (audioId == AudioEventId.None)
            {
                return;
            }

            var volumeMultiplier = loudness > 0f && maxLoudness > 0f
                ? Mathf.Clamp01(loudness / maxLoudness)
                : 1f;

            var audioSignal = new AudioSignal(audioId, transform.position, gameObject, volumeMultiplier);
            DebugSound.GetVolume(audioId, loudness);
            _audioManager.ReportSignal(audioSignal);
            Debug.Log("[SoundEmitter] Local audio reported.");
        }

        [ContextMenu("Emitir som de teste")]
        public void DebugEmitSound()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Only available in Play Mode.");
                return;
            }

            var random = Random.Range(0f, maxLoudness);
            EmitSound(AudioEventId.Test, random * 0.5f);
        }
    }
}
