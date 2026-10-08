using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    [RequireComponent(typeof(SoundEmitter))]
    public class ObjectSoundEmitter : MonoBehaviour
    {
        [Header("Som deste objeto")]
        [SerializeField] private SoundEntry impactSound;
        [SerializeField] private float minImpactForce = 1f;
        [SerializeField] private float maxLoudnessOnImpact = 20f;

        private SoundEmitter _soundEmitter;

        private void Awake() => _soundEmitter = GetComponent<SoundEmitter>();

        public void EmitImpact(float impactSpeed)
        {
            if (impactSpeed < minImpactForce) return;

            var loudness = Mathf.Clamp(impactSpeed * 0.8f, 0f, maxLoudnessOnImpact);
            _soundEmitter.EmitSound(impactSound.id, loudness);
        }
    }
}