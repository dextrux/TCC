using System.Collections.Generic;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using UnityEngine;
using UnityEngine.Audio;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Services
{
    [System.Serializable]
    public class AudioManager : IAudioManager
    {
        private readonly IAudioSourcePool _pool;
        private readonly AudioEventRegistry _registry;
        private readonly AudioMixer _mixer;
        private readonly List<AudioSourcePooled> _activeSources = new();

        private const string ParamSfx = "Sfx";
        private const string ParamVoice = "Voice";
        private const string ParamAmbient = "Environment";
        private const string ParamMusic = "Music";

        public AudioManager(IAudioSourcePool pool, AudioEventRegistry registry, AudioMixer mixer)
        {
            _pool = pool;
            _registry = registry;
            _mixer = mixer;
        }

        public void PlayAtPosition(AudioEvent audioEvent, Vector3 position, float volumeMultiplier = 1f)
        {
            if (audioEvent == null) return;

            var source = _pool.GetSource();
            _activeSources.Add(source);
            source.Play(audioEvent, position, volumeMultiplier);
        }

        public void PlayUI(AudioEvent audioEvent)
        {
            if (audioEvent == null) return;

            var source = _pool.GetSource();
            _activeSources.Add(source);
            source.Play2D(audioEvent);
        }
        
        public void ReportSignal(AudioSignal audioSignal)
        {
            Debug.Log($"Sinal de audio reportado do game object {audioSignal.Source} na posicao {audioSignal.Position} | volume x{audioSignal.VolumeMultiplier:0.00}");

            var audioEvent = _registry.GetById(audioSignal.Id);
            if (audioEvent == null)
            {
                Debug.Log($"AudioEvent {audioSignal.Id} nao encontrado");
                return;
            }

            PlayAtPosition(audioEvent, audioSignal.Position, audioSignal.VolumeMultiplier);
        }

        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            var param = GetMixerParam(category);
            if (string.IsNullOrEmpty(param)) return;

            _mixer.SetFloat(param, LinearToDecibel(volume));
        }

        public void StopAll(AudioCategory category)
        {
            for (var i = _activeSources.Count - 1; i >= 0; i--)
            {
                var src = _activeSources[i];
                if (src == null)
                {
                    _activeSources.RemoveAt(i);
                    continue;
                }

                src.Stop();
                _activeSources.RemoveAt(i);
            }
        }

        private string GetMixerParam(AudioCategory category) => category switch
        {
            AudioCategory.Sfx => ParamSfx,
            AudioCategory.Voice => ParamVoice,
            AudioCategory.Ambient => ParamAmbient,
            AudioCategory.Music => ParamMusic,
            _ => null
        };

        private float LinearToDecibel(float linear) =>
            linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}