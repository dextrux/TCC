using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object
{
    /// <summary>
    /// ESTE SCRIPT DEVE SER COLOCADO NO PREFAB DO OBJETO! 
    /// </summary>
    
    [RequireComponent(typeof(MeshCollider), 
        typeof(Rigidbody),
        typeof(NetworkTransform))]
    
    public class ObjectView : NetworkBehaviour
    {
        private const float SkinWidth = 0.01f;
        
        [Header("Interaction Settings")]
        [SerializeField] private Animator animator;
        private static readonly int OpenHash = Animator.StringToHash("Open");
        private readonly NetworkVariable<bool> _isOpen = new NetworkVariable<bool>(false);
        /// <summary>
        /// Disparado no servidor quando um cliente pede para interagir (clientId do remetente).
        /// </summary>
        public event Action<ulong> InteractRequested;

        private Rigidbody _rigidbody;
        
        public Vector3 Position => _rigidbody.position;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true; // movimento eh calculado pelo Model, nao pela fisica da Unity
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isOpen.OnValueChanged += OnOpenChanged;
            ApplyOpen(_isOpen.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isOpen.OnValueChanged -= OnOpenChanged;
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// Move o objeto (apenas servidor). Retorna false se algo bloqueou o caminho
        /// </summary>
        public bool TryMove(Vector3 displacement, out RaycastHit hit)
        {
            hit = default;

            var distance = displacement.magnitude;
            if (distance <= Mathf.Epsilon) return true;

            var direction = displacement / distance;

            if (_rigidbody.SweepTest(direction, out hit, distance + SkinWidth))
            {
                var safeDistance = Mathf.Max(0f, hit.distance - SkinWidth);
                _rigidbody.MovePosition(_rigidbody.position + direction * safeDistance);
                return false;
            }

            _rigidbody.MovePosition(_rigidbody.position + displacement);
            return true;
        }
        
        private void OnOpenChanged(bool previous, bool current) => ApplyOpen(current);

        public void SetOpen(bool open)
        {
            if (!IsServer) return;
            
            _isOpen.Value = open;
        }

        private void ApplyOpen(bool open)
        {
            if(animator != null)
                animator.SetBool(OpenHash, open);
        }

        [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
        public void InteractServerRpc(ServerRpcParams rpcParams = default)
        {
            InteractRequested?.Invoke(rpcParams.Receive.SenderClientId);
        }
    }
}