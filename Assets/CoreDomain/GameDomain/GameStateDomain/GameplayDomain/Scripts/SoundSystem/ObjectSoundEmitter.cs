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

        private void OnCollisionEnter(Collision collision)
        {
            var impactForce = collision.relativeVelocity.magnitude;
            if (impactForce < minImpactForce) return;

            var loudness = Mathf.Clamp(impactForce * 0.8f, 0f, maxLoudnessOnImpact);
            _soundEmitter.EmitSound(impactSound.id, loudness);
        }
    }
}