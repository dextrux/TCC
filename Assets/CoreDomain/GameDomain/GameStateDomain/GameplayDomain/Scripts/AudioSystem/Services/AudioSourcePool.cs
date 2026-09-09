using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces;
using UnityEngine.Audio;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Services
{
    public class AudioSourcePool : IAudioSourcePool
    {
        private readonly AudioSourcePooled.Factory _factory;
        private readonly AudioMixerGroup _defaultGroup;
        private readonly Queue<AudioSourcePooled> _available = new();
        private readonly List<AudioSourcePooled> _all = new();

        public AudioSourcePool(AudioSourcePooled.Factory factory, AudioMixerGroup defaultGroup)
        {
            _factory = factory;
            _defaultGroup = defaultGroup;
        }

        // Pre-instancia fontes para evitar picos de alocacao em runtime
        public void Prewarm(int count)
        {
            for (var i = 0; i < count; i++)
                _available.Enqueue(CreateNewInstance());
        }

        public AudioSourcePooled GetSource()
        {
            var source = _available.Count > 0 ? _available.Dequeue() : CreateNewInstance();
            source.SetReturnCallback(ReleaseSource);
            return source;
        }

        public void ReleaseSource(AudioSourcePooled source)
        {
            if (source == null) return;
            if (!_available.Contains(source))
                _available.Enqueue(source);
        }

        private AudioSourcePooled CreateNewInstance()
        {
            var instance = _factory.Create();
            _all.Add(instance);
            return instance;
        }
    }
}
