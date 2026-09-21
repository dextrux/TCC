using CoreDomain.Scripts.Services.UpdateService;
using Player.Input;
using Player.Model;
using Player.View;
using Unity.Netcode;
using UnityEngine;

namespace Player.Controller {
    public class FPSPlayerController : IFPSPlayerController, IUpdatable, IFixedUpdatable {
        [Header("References")]
        private FPSPlayerView _view;
        private FPSPlayerConfigurationSO _configuration;
        private FPSPlayerModel _model;
        private PlayerInputController _input;
        private IUpdateSubscriptionService _updateSubscriptionService;

        private float _cameraPitch;

        public FPSPlayerController(FPSPlayerConfigurationSO fPSPlayerConfigurationSO, IUpdateSubscriptionService updateSubscriptionService,
            FPSPlayerView view) {
            _input = new PlayerInputController();
            _model = new FPSPlayerModel();
            _configuration = fPSPlayerConfigurationSO;
            _view = GameObject.Instantiate(view);
            _updateSubscriptionService = updateSubscriptionService;
        }

        public void ManagedUpdate() {
            _input.Update();

            HandleLook();
            HandleJump();
        }

        public void ManagedFixedUpdate() {
            HandleMovement();
            HandleGravity();
        }

        public void SetUp() {
            _view.SetUp();
            _updateSubscriptionService.RegisterUpdatable(this);
            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }

        private void HandleMovement() {
            Vector2 input = _input.MoveInput;

            Vector3 forward = _view.transform.forward;
            Vector3 right = _view.transform.right;

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