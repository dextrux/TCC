using UnityEngine;

namespace Player.Input {
    public class PlayerInputController : IPlayerInput {
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        public bool JumpPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool ThrowPressed { get; private set; }

        public void Update() {
            ReadMovement();
            ReadLook();
            ReadJump();
            ReadActions();
        }

        public void ConsumeJump() {
            JumpPressed = false;
        }
        
        public void ConsumeInteract() => InteractPressed = false;
        public void ConsumeThrow() => ThrowPressed = false;

        private void ReadMovement() {
            float horizontal = 0f;
            float vertical = 0f;

            if (UnityEngine.Input.GetKey(KeyCode.A) ||
                UnityEngine.Input.GetKey(KeyCode.LeftArrow)) {
                horizontal -= 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.D) ||
                UnityEngine.Input.GetKey(KeyCode.RightArrow)) {
                horizontal += 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.W) ||
                UnityEngine.Input.GetKey(KeyCode.UpArrow)) {
                vertical += 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.S) ||
                UnityEngine.Input.GetKey(KeyCode.DownArrow)) {
                vertical -= 1f;
            }

            MoveInput = new Vector2(horizontal, vertical);

            MoveInput = Vector2.ClampMagnitude(MoveInput, 1f);
        }

        private void ReadLook() {
            float mouseX = UnityEngine.Input.GetAxis("Mouse X");
            float mouseY = UnityEngine.Input.GetAxis("Mouse Y");

            LookInput = new Vector2(mouseX, mouseY);
        }

        private void ReadJump() {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) {
                JumpPressed = true;
            }
        }

        private void ReadActions()
        {
            if(UnityEngine.Input.GetKeyDown(KeyCode.E)) InteractPressed = true;
            if(UnityEngine.Input.GetKeyDown(KeyCode.Mouse0)) ThrowPressed = true;
        }
    }
}