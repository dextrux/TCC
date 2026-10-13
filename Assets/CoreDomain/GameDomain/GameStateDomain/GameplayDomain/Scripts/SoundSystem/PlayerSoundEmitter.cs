using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    [RequireComponent(typeof(SoundEmitter))]
    public class PlayerSoundEmitter : MonoBehaviour
    {
        [Header("Acoes do Player")]
        [SerializeField] private SoundEntry walkingSound;
        [SerializeField] private SoundEntry runningSound;
        [SerializeField] private SoundEntry crouchingSound;
        [SerializeField] private SoundEntry crawlingSound;
        [SerializeField] private SoundEntry jumpingSound;

        private SoundEmitter _soundEmitter;

        private void Awake() => _soundEmitter = GetComponent<SoundEmitter>();

        public void EmitWalkingOnAnimation(bool walking) =>
            _soundEmitter.EmitSound(walkingSound.id, walking ? walkingSound.loudness : 0f);

        public void EmitRunningOnAnimation(bool running) =>
            _soundEmitter.EmitSound(runningSound.id, running ? runningSound.loudness : 0f);

        public void EmitCrouchingOnAnimation(bool crouching) =>
            _soundEmitter.EmitSound(crouchingSound.id, crouching ? crouchingSound.loudness : 0f);

        public void EmitCrawlingOnAnimation() =>
            _soundEmitter.EmitSound(crawlingSound.id, crawlingSound.loudness);
        
        public void EmitJumpingOnAnimation() =>
            _soundEmitter.EmitSound(jumpingSound.id, jumpingSound.loudness);
    }
}