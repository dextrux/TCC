using Mirror;
using UnityEngine;

public class MirrorPlayerCamera : NetworkBehaviour
{
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

    [SyncVar(hook = nameof(OnServerYawChanged))]
    private float _serverYaw;

    [SyncVar(hook = nameof(OnServerPitchChanged))]
    private float _serverPitch;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _serverYaw = transform.eulerAngles.y;

        if (cameraPivot != null)
        {
            float initialPitch = cameraPivot.localEulerAngles.x;

            if (initialPitch > 180f)
            {
                initialPitch -= 360f;
            }

            _serverPitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
        }

        ApplyCameraRotation(_serverYaw, _serverPitch);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        SetCameraActive(false);
        ApplyCameraRotation(_serverYaw, _serverPitch);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        ActivateLocalCamera();
        HideOwnerVisuals();
    }

    [Server]
    public void ServerApplyCameraInput(Vector2 cameraInput)
    {
        if (float.IsNaN(cameraInput.x) || float.IsNaN(cameraInput.y))
        {
            return;
        }

        if (float.IsInfinity(cameraInput.x) || float.IsInfinity(cameraInput.y))
        {
            return;
        }

        float inputX = Mathf.Clamp(cameraInput.x, -maxCameraInputPerMessage, maxCameraInputPerMessage);
        float inputY = Mathf.Clamp(cameraInput.y, -maxCameraInputPerMessage, maxCameraInputPerMessage);

        _serverYaw = Mathf.Repeat(_serverYaw + inputX * cameraSensitivity, 360f);
        _serverPitch = Mathf.Clamp(_serverPitch - inputY * cameraSensitivity, minPitch, maxPitch);

        ApplyCameraRotation(_serverYaw, _serverPitch);
    }

    private void OnServerYawChanged(float oldYaw, float newYaw)
    {
        ApplyCameraRotation(newYaw, _serverPitch);
    }

    private void OnServerPitchChanged(float oldPitch, float newPitch)
    {
        ApplyCameraRotation(_serverYaw, newPitch);
    }

    private void ApplyCameraRotation(float yaw, float pitch)
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void ActivateLocalCamera()
    {
        Camera currentMainCamera = Camera.main;

        if (currentMainCamera != null && currentMainCamera != playerCamera)
        {
            currentMainCamera.enabled = false;

            if (currentMainCamera.CompareTag("MainCamera"))
            {
                currentMainCamera.tag = "Untagged";
            }
        }

        AudioListener[] audioListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioListener currentListener in audioListeners)
        {
            currentListener.enabled = currentListener == playerAudioListener;
        }

        SetCameraActive(true);

        if (playerCamera != null)
        {
            playerCamera.tag = "MainCamera";
        }
    }

    private void SetCameraActive(bool active)
    {
        if (playerCamera != null)
        {
            playerCamera.enabled = active;

            if (!active && playerCamera.CompareTag("MainCamera"))
            {
                playerCamera.tag = "Untagged";
            }
        }

        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = active;
        }
    }

    private void HideOwnerVisuals()
    {
        if (ownerHiddenRenderers == null)
        {
            return;
        }

        foreach (Renderer currentRenderer in ownerHiddenRenderers)
        {
            if (currentRenderer != null)
            {
                currentRenderer.enabled = false;
            }
        }
    }
}