using System;
using Mirror;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object
{
    [RequireComponent(typeof(NetworkIdentity),
        typeof(Rigidbody),
        typeof(MeshCollider))]
    [RequireComponent(typeof(Animator))]
    public class ObjectView : NetworkBehaviour
    {
        private const float SkinWidth = 0.1f;
        
        [Header("Interact Settings")]
        [SerializeField] private Animator animator;
        private static readonly int OpenHash = Animator.StringToHash("Open");
        
        [SyncVar(hook = "OnOpenChanged")] private bool _isOpen;
        [SyncVar] private bool _isHeld;
        
        public bool IsHeld => _isHeld;

        //Quando cliente pedir para interagir
        public event Action<int> InteractRequested;
        public event Action<Vector3, Vector3> ThrowRequested;
        
        private Rigidbody _rigidbody;
        private MeshCollider _meshCollider;
        
        public Vector3 Position => _rigidbody.position;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _meshCollider = GetComponent<MeshCollider>();
            animator = GetComponent<Animator>();
            _rigidbody.isKinematic = true;
            _meshCollider.convex = true;

        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyOpen(_isOpen);
        }

        //Servidor move o objeto - Retorna false se bateu em algo
        public bool TryMove(Vector3 displacement, out RaycastHit hit)
        {
            hit = default;
            
            var distance = displacement.magnitude;
            if (distance <= Mathf.Epsilon) return true;
            
            var direction = displacement / distance;

            if (_rigidbody.SweepTest(direction, out hit, distance + SkinWidth))
            {
                var safeDistance = Mathf.Max(0f, hit.distance - SkinWidth);
                _rigidbody.MovePosition(transform.position + direction * safeDistance);
                return false;
            }
            
            _rigidbody.MovePosition(transform.position + displacement);
            return true;
        }

        public void Teleport(Vector3 position)
        {
            if (!isServer) return;
            
            _rigidbody.position = position;
            _rigidbody.rotation = Quaternion.identity;
        }

        private void OnOpenChanged(bool previous, bool current) => ApplyOpen(current);
        
        public void SetHeld(bool held)
        {
            if (!isServer) return;
            _isHeld = held;
        }
        
        public void SetOpen(bool open)
        {
            if(!isServer) return;
            _isOpen = open;
        }
        
        private void ApplyOpen(bool open)
        {
            if(animator.runtimeAnimatorController == null) return;
            animator.SetBool(OpenHash, open);
        }

        [Command(requiresAuthority = false)]
        public void CmdInteract(NetworkConnectionToClient sender = null)
        {
            if (sender == null) return;
            InteractRequested?.Invoke(sender.connectionId);
        }
        
        [Command]
        public void CmdThrow(Vector3 origin, Vector3 direction) => ThrowRequested?.Invoke(origin, direction);
    }
}