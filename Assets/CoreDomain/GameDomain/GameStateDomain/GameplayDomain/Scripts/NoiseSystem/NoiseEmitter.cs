
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.Scripts.Services.NetworkService;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem
{
    public class NoiseEmitter : MonoBehaviour, INoiseEmitter
    {
#region Loud Variables
        [SerializeField] private float walkingLoudness = 5f;
        [SerializeField] private float runningLoudness = 12f;
        [SerializeField] private float crouchingLoudness = 2f;
        [SerializeField] private float crawlingLoudness = 0f;
#endregion Loud Variables

        [SerializeField] private bool serverOnly;

        private INetworkSoundService _networkSoundService;

        [Inject]
        public void Construct(INetworkSoundService networkSoundService)
        {
            _networkSoundService = networkSoundService;
        }

        public void EmitNoise(float loudness)
        {
            if (_networkSoundService == null)
            {
                Debug.LogError("[NoiseEmitter] INetworkSoundService was not injected.");
                return;
            }

            if (float.IsNaN(loudness) || float.IsInfinity(loudness) || loudness <= 0f)
            {
                return;
            }

            if (serverOnly)
            {
                _networkSoundService.EmitServerSound((int)AudioEventId.None, loudness, transform.position, gameObject);
                return;
            }

            _networkSoundService.RequestSound((int)AudioEventId.None, loudness, transform.position);
        }

        [ContextMenu("Emitir ruido de teste")]
        private void DebugEmitNoise()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                Debug.LogWarning("So funciona em Play Mode.");
#endif
                return;
            }

            EmitNoise(1f);
        }

#region AnimationEvents
        public void EmitWalkingOnAnimation(bool walking) => EmitNoise(walking ? walkingLoudness : 0);
        public void EmitRunningOnAnimation(bool running) => EmitNoise(running ? runningLoudness : 0);
        public void EmitCrouchingOnAnimation(bool crouching) => EmitNoise(crouching ? crouchingLoudness : 0);
        public void EmitCrawlingOnAnimation() => EmitNoise(crawlingLoudness);
#endregion AnimationEvents
    }
}
