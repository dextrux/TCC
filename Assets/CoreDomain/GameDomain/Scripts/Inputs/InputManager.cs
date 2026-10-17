using Unity.Netcode;
using UnityEngine;
using Mirror;
using NetworkClient = Mirror.NetworkClient;
using NetworkBehaviour = Mirror.NetworkBehaviour;
using UnityEngine.InputSystem;
using System;

namespace CoreDomain.GameDomain.Scripts.Inputs {
    public class InputManager : NetworkBehaviour, IInputManager {
        [SerializeField] private GameInputActions _playerInputActions;

        public void ChangeToPLayerInput() {
            //Setup();
            _playerInputActions.UI.Disable();
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

        public void ChangeToUIInput() {
            _playerInputActions.Player.Disable();
            _playerInputActions.UI.Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnLook(InputAction.CallbackContext obj) {
            PlayerCameraInputMessage cameraMessage = new PlayerCameraInputMessage();
            cameraMessage.CameraInput = obj.ReadValue<Vector2>();
            NetworkClient.Send(cameraMessage, Channels.Unreliable);
        }

        private void OnMove(InputAction.CallbackContext obj) {
            PlayerCameraInputMessage cameraMessage = new PlayerCameraInputMessage();
            cameraMessage.CameraInput = obj.ReadValue<Vector2>();
            NetworkClient.Send(cameraMessage, Channels.Unreliable);
        }


        public void Setup() {
            _playerInputActions = new GameInputActions();
            ChangeToUIInput();
        }
    }
}