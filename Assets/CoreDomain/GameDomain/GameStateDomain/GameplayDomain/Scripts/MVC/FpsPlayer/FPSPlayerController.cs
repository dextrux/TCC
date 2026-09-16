using Player.Input;
using Player.Model;
using Player.View;
using UnityEngine;

namespace Player.Controller {
    public class FPSPlayerController : MonoBehaviour {
        [Header("References")]
        [SerializeField]
        private FPSPlayerView _view;

        [SerializeField]
        private FPSPlayerConfigurationSO _configuration;

        private FPSPlayerModel _model;
        private PlayerInputController _input;

        private float _cameraPitch;

        private void Awake() {
            _model = new FPSPlayerModel();

            _model = new FPSPlayerModel();
            _input = new PlayerInputController();
        }

        private void Update() {
            _input.Update();

            HandleLook();
            HandleJump();
        }

        private void FixedUpdate() {
            HandleMovement();
            HandleGravity();
        }

        private void HandleMovement() {
            Vector2 input = _input.MoveInput;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            Vector3 direction =
                forward * input.y +
                right * input.x;

            direction = Vector3.ClampMagnitude(direction, 1f);

            Vector3 horizontalVelocity =
                direction * _configuration.MoveSpeed;

            _model.SetVelocity(
                new Vector3(
                    horizontalVelocity.x,
                    _model.Velocity.y,
                    horizontalVelocity.z
                )
            );

            Vector3 movement =
                _model.Velocity * Time.fixedDeltaTime;

            _view.Move(movement);
        }

        private void HandleGravity() {
            if (_view.IsGrounded() &&
                _model.Velocity.y < 0f) {
                _model.ResetVerticalVelocity();
            }

            _model.ApplyGravity(
                _configuration.Gravity,
                Time.fixedDeltaTime
            );
        }

        private void HandleJump() {
            if (!_input.JumpPressed)
                return;

            if (_view.IsGrounded()) {
                Vector3 velocity = _model.Velocity;

                velocity.y = _configuration.JumpForce;

                _model.SetVelocity(velocity);
            }

            _input.ConsumeJump();
        }

        private void HandleLook() {
            Vector2 look = _input.LookInput;

            float yaw =
                look.x *
                _configuration.LookSensitivity;

            float pitch =
                look.y *
                _configuration.LookSensitivity;

            _view.RotateBody(yaw);

            _cameraPitch -= pitch;

            _cameraPitch = Mathf.Clamp(
                _cameraPitch,
                _configuration.MinPitch,
                _configuration.MaxPitch
            );

            _view.RotateCamera(_cameraPitch);
        }
    }
}