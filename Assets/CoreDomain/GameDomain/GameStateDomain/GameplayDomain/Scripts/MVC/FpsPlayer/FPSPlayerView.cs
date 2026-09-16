using UnityEngine;

namespace Player.View {
    public class FPSPlayerView : MonoBehaviour {
        [field: SerializeField]
        public CharacterController CharacterController { get; private set; }

        [field: SerializeField]
        public Transform CameraTransform { get; private set; }

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
    }
}