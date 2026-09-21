using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
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
            Debug.Log("Constructing AudioUi");
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) PlayUI();
            if (Input.GetKeyDown(KeyCode.F2)) StopUiAudio();
        }
    }
}