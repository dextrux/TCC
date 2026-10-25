
using UnityEngine;
using Mirror;
using NetworkClient = Mirror.NetworkClient;
using NetworkBehaviour = Mirror.NetworkBehaviour;
using UnityEngine.InputSystem;

namespace CoreDomain.GameDomain.Scripts.Inputs {
    public class InputManager : NetworkBehaviour, IInputManager {
        [SerializeField] private GameInputActions _playerInputActions;

        public void EnablePLayerInput() {
            _playerInputActions.Player.Enable();
            _playerInputActions.Player.Look.started += OnLook;
            _playerInputActions.Player.Look.performed += OnLook;
            _playerInputActions.Player.Look.canceled += OnLook;
            _playerInputActions.Player.Move.started += OnMove;
            _playerInputActions.Player.Move.performed += OnMove;
            _playerInputActions.Player.Move.canceled += OnMove;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void DisablePLayerInput() {
            _playerInputActions.Player.Look.started -= OnLook;
            _playerInputActions.Player.Look.performed -= OnLook;
            _playerInputActions.Player.Look.canceled -= OnLook;
            _playerInputActions.Player.Move.started -= OnMove;
            _playerInputActions.Player.Move.performed -= OnMove;
            _playerInputActions.Player.Move.canceled -= OnMove;
            _playerInputActions.Player.Disable();
        }

        public void EnableUIInput() {
            _playerInputActions.UI.Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void DisableUIInput() {
            _playerInputActions.UI.Disable();
        }

        private void OnLook(InputAction.CallbackContext obj) {
            if (!NetworkClient.isConnected || !NetworkClient.ready || NetworkClient.localPlayer == null) {
                return;
            }
            PlayerCameraInputMessage cameraMessage = new PlayerCameraInputMessage();
            cameraMessage.CameraInput = obj.ReadValue<Vector2>();
            NetworkClient.Send(cameraMessage, Channels.Unreliable);
        }

        private void OnMove(InputAction.CallbackContext obj) {
            if (!NetworkClient.isConnected || !NetworkClient.ready || NetworkClient.localPlayer == null) {
                return;
            }
            
            PlayerMovementInputMessage movementMessage = new PlayerMovementInputMessage();
            movementMessage.MovementInput = obj.ReadValue<Vector2>();
            Debug.LogWarning("Input Move: " + obj.ReadValue<Vector2>());
            NetworkClient.Send(movementMessage, Channels.Unreliable);
        }

        public void Setup() {
            _playerInputActions = new GameInputActions();
        }
    }
}
