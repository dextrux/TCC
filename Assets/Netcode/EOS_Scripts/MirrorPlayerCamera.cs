using Mirror;
using UnityEngine;

public class MirrorPlayerCamera : NetworkBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;

    [Header("Owner Visual")]
    [SerializeField] private Renderer[] ownerHiddenRenderers;

    public override void OnStartClient()
    {
        base.OnStartClient();

        SetCameraActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        ActivateLocalCamera();
        HideOwnerVisuals();
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