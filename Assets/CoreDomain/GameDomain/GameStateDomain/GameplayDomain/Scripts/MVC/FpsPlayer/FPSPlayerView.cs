using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object;
using Unity.Netcode;
using UnityEngine;

namespace Player.View {
    public class FPSPlayerView : NetworkBehaviour {
        [field: SerializeField]
        public CharacterController CharacterController { get; private set; }

        [field: SerializeField]
        public Transform CameraTransform { get; private set; }
        [field: SerializeField]
        private Transform CameraPointTransform;
        
        public Vector3 ThrowOrigin => CameraTransform.position + CameraTransform.forward * 0.5f;
        public Vector3 CameraForward => CameraTransform.forward; 

        public void SetUp() {
            CameraTransform = Camera.main.transform;
            CameraTransform.SetParent(CameraPointTransform);
            CameraTransform.localPosition = Vector3.zero;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Move(Vector3 motion) {
            CharacterController.Move(motion);
        }

        public bool IsGrounded() {
            return CharacterController.isGrounded;
        }

        public void RotateBody(float yaw) {
            transform.Rotate(Vector3.up * yaw);
        }

        public void RotateCamera(float pitch) {
            CameraTransform.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }

        public bool TryGetLookedObject(float range, out ObjectView target)
        {
            target = null;
            if(!Physics.Raycast(CameraTransform.position, CameraTransform.forward, out var hit, range))
                return false;
            
            target = hit.collider.GetComponentInParent<ObjectView>();
            return target != null;
        }

        public bool TryGetHeldObject(out ObjectView held)
        {
            held = null;
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening) return false;

            foreach (var networkObject in networkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (!networkObject.IsOwner || !networkObject.TryGetComponent<ObjectView>(out var view) || !view.IsHeld) continue;
                held = view;
                return true;
            }
            return false;
        }
    }
}