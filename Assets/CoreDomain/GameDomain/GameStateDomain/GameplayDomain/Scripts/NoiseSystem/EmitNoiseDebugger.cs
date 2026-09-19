using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem
{
    public class EmitNoiseDebugger : NetworkBehaviour, INoiseEmitter
    {
        /// <summary>
        /// Debug criado para que possa ser testado a emissao de barulho
        /// e assim da para testar todos os casos de barulhos
        /// POSSUI AS MESMAS FUNCOES QUE O NOISE EMITTER
        ///
        /// Para que possa ser utilizado basta clicar no script dentro do objeto com o botao direito
        /// e clicar em DebugEmitNoise().
        /// </summary>

        [SerializeField, Range(0f, 1f)] private float testNoiseDebugger;
        
        private INoiseManager _noiseManager;

        [Inject]
        public void Construct(INoiseManager noiseManager)
        {
            _noiseManager = noiseManager;
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"{name} spawnou na rede. IsSpawned={IsSpawned}, IsOwner={IsOwner}, IsServer={IsServer}");
        }
        
        public void EmitNoise(float loudness)
        {
            if (!IsOwner)
            {
                Debug.Log("Nao foi possivel emitir ruido pois nao eh o Owner");
                return;
            }
            
            EmitNoiseServerRpc(loudness, transform.position);
        }
        
        [ServerRpc]
        private void EmitNoiseServerRpc(float loudness, Vector3 position)
        {
            Debug.Log("Emitindo barulho ServerRpc");
            var signal = new NoiseSignal(position, loudness, gameObject);
            _noiseManager.ReportSignal(signal); // roda no servidor
        }
        
        [ContextMenu("Emitir Ruído de Teste")]
        private void DebugEmitNoise()
        {
            if (!Application.isPlaying || !IsSpawned)
            {
                Debug.LogWarning("So funciona em Play Mode com o objeto spawnado.");
                return;
            }

            EmitNoise(testNoiseDebugger);
        }
    }
}