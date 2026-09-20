using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.Scripts.AudioSystem
{
    public class AudioUi : MonoBehaviour
    {
        [SerializeField] private AudioEvent audioEvent;
        
        private IAudioManager _audioManager;

        [Inject]
        public void Construct(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }
        
        public void PlayUI()
        {
            _audioManager.PlayUI(audioEvent: this.audioEvent);
        }

        public void StopUiAudio()
        {
             _audioManager.StopAll(AudioCategory.Ui);
        }
    }
}