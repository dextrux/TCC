using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourcePooled : MonoBehaviour
    {
        public class Factory : Zenject.PlaceholderFactory<AudioSourcePooled> { }

        [SerializeField] private AudioSource source;
        
        private Action<AudioSourcePooled> _onFinished;

        public bool IsPlaying => source != null && source.isPlaying;
        public bool IsFree { get; private set; } = true;

        private void Reset() => source = GetComponent<AudioSource>();

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
        }

        //Trocar pro novo update
        private void Update()
        {
            if (!IsFree && !source.loop && !source.isPlaying)
                ReturnToPool();
        }

        public void Play(AudioEvent audioEvent, Vector3 position, Transform parent = null)
        {
            if (audioEvent == null) return;

            IsFree = false;
            transform.position = position;

            ConfigureFor3D(audioEvent);
            ApplyCommonSettings(audioEvent);
            source.Play();
        }

        public void Play2D(AudioEvent audioEvent)
        {
            if (audioEvent == null) return;

            IsFree = false;

            source.spatialBlend = 0f;
            ApplyCommonSettings(audioEvent);
            source.Play();
        }

        public void Stop()
        {
            if (source.isPlaying) source.Stop();
            ReturnToPool();
        }

        public void SetReturnCallback(Action<AudioSourcePooled> onFinished) => _onFinished = onFinished;

        public void ReturnToPool()
        {
            if (IsFree) return;

            IsFree = true;
            source.clip = null;
            _onFinished?.Invoke(this);
        }

        private void ConfigureFor3D(AudioEvent audioEvent)
        {
            source.spatialBlend = audioEvent.Is3D ? 1f : 0f;
            source.spread = audioEvent.Spread; 
            source.minDistance = audioEvent.MinDistance;
            source.maxDistance = audioEvent.MaxDistance;
            source.rolloffMode = AudioRolloffMode.Custom;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioEvent.RolloffCurve);
        }

        private void ApplyCommonSettings(AudioEvent audioEvent)
        {
            source.clip = audioEvent.GetClip();
            source.volume = audioEvent.Volume;
            source.pitch = audioEvent.GetRandomPitch();
            source.loop = audioEvent.Loop;
        }
    }
}
