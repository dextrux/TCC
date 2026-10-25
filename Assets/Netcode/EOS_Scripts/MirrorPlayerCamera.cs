using CoreDomain.Scripts.Services.UpdateService;
using Mirror;
using UnityEngine;
using Zenject;

public class MirrorPlayerCamera : NetworkBehaviour {
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;
    [SerializeField] private Transform cameraPivot;

    [Header("Camera Movement")]
    [SerializeField] private float cameraSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float maxCameraInputPerMessage = 100f;

    [Header("Owner Visual")]
    [SerializeField] private Renderer[] ownerHiddenRenderers;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private CharacterController characterController;

    private Vector3 velocity;
    private bool isLocal = false;

    [SyncVar(hook = nameof(OnServerYawChanged))]
    private float _serverYaw;

    [SyncVar(hook = nameof(OnServerPitchChanged))]
    private float _serverPitch;


    public override void OnStartServer() {
        base.OnStartServer();

        _serverYaw = transform.eulerAngles.y;

        if (cameraPivot != null) {
            float initialPitch = cameraPivot.localEulerAngles.x;

            if (initialPitch > 180f) {
                initialPitch -= 360f;
            }

            _serverPitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
        }

        ApplyCameraRotation(_serverYaw, _serverPitch);
    }

    public override void OnStartClient() {
        base.OnStartClient();

        SetCameraActive(false);
        ApplyCameraRotation(_serverYaw, _serverPitch);
    }

    public override void OnStartLocalPlayer() {
        base.OnStartLocalPlayer();

        ActivateLocalCamera();
        HideOwnerVisuals();
        isLocal = true;
    }

    [Server]
    public void ServerApplyCameraInput(Vector2 cameraInput) {
        if (float.IsNaN(cameraInput.x) || float.IsNaN(cameraInput.y)) {
            return;
        }

        if (float.IsInfinity(cameraInput.x) || float.IsInfinity(cameraInput.y)) {
            return;
        }

        float inputX = Mathf.Clamp(cameraInput.x, -maxCameraInputPerMessage, maxCameraInputPerMessage);
        float inputY = Mathf.Clamp(cameraInput.y, -maxCameraInputPerMessage, maxCameraInputPerMessage);

        _serverYaw = Mathf.Repeat(_serverYaw + inputX * cameraSensitivity, 360f);
        _serverPitch = Mathf.Clamp(_serverPitch - inputY * cameraSensitivity, minPitch, maxPitch);

        ApplyCameraRotation(_serverYaw, _serverPitch);
    }

    [Server]
    public void ServerApplyMovementInput(Vector2 movementInput) {
        if (characterController == null) return;

        if (float.IsNaN(movementInput.x) || float.IsNaN(movementInput.y)) return;
        if (float.IsInfinity(movementInput.x) || float.IsInfinity(movementInput.y)) return;

        Vector3 inputDir = new Vector3(movementInput.x, 0f, movementInput.y).normalized;
        Vector3 moveDir = transform.TransformDirection(inputDir);
        Vector3 movement = moveDir * moveSpeed;
        //Gravity
        if (characterController.isGrounded) {
            velocity.y = -2f;
        }
        else {
            velocity.y += gravity * Time.deltaTime;
        }
        movement.y = velocity.y;
        velocity = movement;
        Debug.LogWarning("Player Move: " + movement);
    }

    private void FixedUpdate() {
        //if (isLocal && !isServer) return;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void OnServerYawChanged(float oldYaw, float newYaw) {
        ApplyCameraRotation(newYaw, _serverPitch);
    }

    private void OnServerPitchChanged(float oldPitch, float newPitch) {
        ApplyCameraRotation(_serverYaw, newPitch);
    }

    private void ApplyCameraRotation(float yaw, float pitch) {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraPivot != null) {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void ActivateLocalCamera() {
        Camera currentMainCamera = Camera.main;

        if (currentMainCamera != null && currentMainCamera != playerCamera) {
            currentMainCamera.enabled = false;

            if (currentMainCamera.CompareTag("MainCamera")) {
                currentMainCamera.tag = "Untagged";
            }
        }

        AudioListener[] audioListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioListener currentListener in audioListeners) {
            currentListener.enabled = currentListener == playerAudioListener;
        }

        SetCameraActive(true);

        if (playerCamera != null) {
            playerCamera.tag = "MainCamera";
        }
    }

    private void SetCameraActive(bool active) {
        if (playerCamera != null) {
            playerCamera.enabled = active;

            if (!active && playerCamera.CompareTag("MainCamera")) {
                playerCamera.tag = "Untagged";
            }
        }

        if (playerAudioListener != null) {
            playerAudioListener.enabled = active;
        }
    }

    private void HideOwnerVisuals() {
        if (ownerHiddenRenderers == null) {
            return;
        }

        foreach (Renderer currentRenderer in ownerHiddenRenderers) {
            if (currentRenderer != null) {
                currentRenderer.enabled = false;
            }
        }
    }
}