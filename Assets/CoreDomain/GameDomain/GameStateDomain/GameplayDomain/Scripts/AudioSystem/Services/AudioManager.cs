using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces;
using UnityEngine;
using UnityEngine.Audio;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Services
{
    [System.Serializable]
    public class AudioManager : IAudioManager
    {
        private readonly IAudioSourcePool _pool;
        private readonly AudioEventRegistry _registry;
        private readonly AudioMixer _mixer;
        private readonly List<AudioSourcePooled> _activeSources = new();

        private const string ParamSfx = "SFXVolume";
        private const string ParamVoice = "VoiceVolume";
        private const string ParamAmbient = "AmbientVolume";
        private const string ParamMusic = "MusicVolume";

        public AudioManager(IAudioSourcePool pool, AudioEventRegistry registry, AudioMixer mixer)
        {
            _pool = pool;
            _registry = registry;
            _mixer = mixer;
        }

        //Toca o audio naquela posicao
        public void PlayAtPosition(AudioEvent audioEvent, Vector3 position)
        {
            if (audioEvent == null) return;

            var source = _pool.GetSource();
            _activeSources.Add(source);
            source.Play(audioEvent, position);
        }

        //Toca o audio na UI
        public void PlayUI(AudioEvent audioEvent)
        {
            if (audioEvent == null) return;

            var source = _pool.GetSource();
            _activeSources.Add(source);
            source.Play2D(audioEvent);
        }

        //Reporta o sinal detectado
        public void ReportSignal(AudioSignal audioSignal)
        {
            Debug.Log($"Sinal de audio reportado do game object {audioSignal.Source} na posicao {audioSignal.Position}");

            var audioEvent = _registry.GetById(audioSignal.Id);
            if (audioEvent == null) return;

            PlayAtPosition(audioEvent, audioSignal.Position);
        }

        //Ajusta o volume no Mixer
        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            var param = GetMixerParam(category);
            if (string.IsNullOrEmpty(param)) return;

            _mixer.SetFloat(param, LinearToDecibel(volume));
        }

        //Para todos os sons ativos em uma categoria
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
