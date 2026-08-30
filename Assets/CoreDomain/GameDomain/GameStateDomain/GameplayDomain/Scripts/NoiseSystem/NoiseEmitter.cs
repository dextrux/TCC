using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem
{
    public class NoiseEmitter : NetworkBehaviour, INoiseEmitter
    {
        [SerializeField] private float walkingLoudness;
        [SerializeField] private float runningLoudness;
        [SerializeField] private float crouchingLoudness;
        [SerializeField] private float crawlingLoudness = 0f;
        
        private INoiseManager _noiseManager;

        [Inject]
        public void Construct(INoiseManager noiseManager)
        {
            _noiseManager = noiseManager;
        }
        
        public override void OnNetworkSpawn()
        {
#if UNITY_EDITOR
            Debug.Log($"{name} spawnou na rede. IsSpawned={IsSpawned}, IsOwner={IsOwner}, IsServer={IsServer}");
#endif
        }
        
        public void EmitNoise(float loudness)
        {
            if (!IsOwner)
            {
#if UNITY_EDITOR
                Debug.Log("Nao foi possivel emitir ruido pois nao eh o Owner");
#endif
                return;
            }
            
            EmitNoiseServerRpc(loudness, transform.position);
        }
        
        [ServerRpc]
        private void EmitNoiseServerRpc(float loudness, Vector3 position)
        {
            var signal = new NoiseSignal(position, loudness, gameObject);
            _noiseManager.ReportSignal(signal);
        }
        
        [ContextMenu("Emitir ruido de teste")]
        private void DebugEmitNoise()
        {
            if (!Application.isPlaying || !IsSpawned)
            {
#if UNITY_EDITOR
                Debug.LogWarning("So funciona em Play Mode com o objeto spawnado.");
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