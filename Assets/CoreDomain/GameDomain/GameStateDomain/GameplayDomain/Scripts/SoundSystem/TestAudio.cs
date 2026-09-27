using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    public class TestAudio : MonoBehaviour
    {
        [SerializeField] private SoundEmitter audioObject;
        private IAudioManager _audioManager;

        [Inject]
        public void Construct(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        private void PlayUI()
        {
            audioObject = FindAnyObjectByType(typeof(SoundEmitter)) as SoundEmitter;
            audioObject!.DebugEmitSound();
        }

        private void StopUiAudio()
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