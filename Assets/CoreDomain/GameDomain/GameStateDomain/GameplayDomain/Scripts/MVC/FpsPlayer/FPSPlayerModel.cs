using UnityEngine;

namespace Player.Model {
    public class FPSPlayerModel {
        public Vector3 Velocity { get; private set; }

        public void SetVelocity(Vector3 velocity) {
            Velocity = velocity;
        }

        public void ApplyGravity(float gravity, float deltaTime) {
            Velocity += Vector3.up * gravity * deltaTime;
        }

        public void ResetVerticalVelocity() {
            Velocity = new Vector3(
                Velocity.x,
                0f,
                Velocity.z
            );
        }
    }
}