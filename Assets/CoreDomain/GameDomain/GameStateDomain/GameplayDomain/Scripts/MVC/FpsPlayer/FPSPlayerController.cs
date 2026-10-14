using CoreDomain.Scripts.Services.UpdateService;
using Player.Input;
using Player.Model;
using Player.View;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Player.Controller {
    public class FPSPlayerController : IFPSPlayerController, IUpdatable, IFixedUpdatable {
        [Header("References")]
        private FPSPlayerView _view;
        private FPSPlayerConfigurationSO _configuration;
        private FPSPlayerModel _model;
        private PlayerInputController _input;
        private IUpdateSubscriptionService _updateSubscriptionService;
        
        //RETIRAR DEPOIS QUE SAIR DO FAKE ONLINE
        private readonly DiContainer _container;

        private float _cameraPitch;

        public FPSPlayerController(FPSPlayerConfigurationSO fPSPlayerConfigurationSO, IUpdateSubscriptionService updateSubscriptionService,
            DiContainer container, FPSPlayerView view) {
            _input = new PlayerInputController();
            _model = new FPSPlayerModel();
            _configuration = fPSPlayerConfigurationSO;
            //_view = GameObject.Instantiate(view);
            _updateSubscriptionService = updateSubscriptionService;
            _container = container;
            
            _view = _container.InstantiatePrefabForComponent<FPSPlayerView>(view);
        }

        public void ManagedUpdate() {
            _input.Update();

            HandleLook();
            HandleJump();
            HandleInteract();
            HandleThrow();
        }

        public void ManagedFixedUpdate() {
            HandleMovement();
            HandleGravity();
        }

        public void SetUp() {
            SpawnPlayerIfNeeded();
            _view.SetUp();
            _updateSubscriptionService.RegisterUpdatable(this);
            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }
        
        /// <summary>
        /// RETIRAR APOS ESTAR EM REDE
        /// </summary>
        private void SpawnPlayerIfNeeded() {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer) return;

            var networkObject = _view.GetComponent<NetworkObject>();
            if (networkObject.IsSpawned) return;
            
            networkObject.SpawnAsPlayerObject(networkManager.LocalClientId);
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

        private void HandleInteract()
        {
            if (!_input.InteractPressed) return;
            
            _input.ConsumeInteract();
            
            if(_view.TryGetLookedObject(_configuration.InteractionRange, out var target))
                target.InteractServerRpc();
        }

        public void HandleThrow()
        {
            if (!_input.ThrowPressed) return;
            _input.ConsumeThrow();
            
            Debug.Log("Throw");
            
            if(_view.TryGetHeldObject(out var held))
                held.ThrowServerRpc(_view.ThrowOrigin, _view.CameraForward);
        }
    }
}