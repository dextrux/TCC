using UnityEngine;
using UnityEngine.Audio;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas
{
    [System.Serializable]
    public class AudioSetting
    {
        [Header("Prefabs")]
        public AudioSourcePooled sourcePrefab;
        
        [Header("Mixer")]
        public AudioMixer mixer;
        public AudioMixerGroup defaultGroup;

        [Header("Data")]
        public AudioEventRegistry registry;
        
        [Header("Network")]
        public NetworkAudioRelay networkAudioRelay;

        [Header("Pool")]
        public int prewarmCount = 16;
    }
}