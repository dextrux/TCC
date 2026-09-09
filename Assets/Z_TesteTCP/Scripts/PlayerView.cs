using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("Camera")]
    public Transform pitchPivot;
    public Camera playerCamera;
    public AudioListener audioListener;

    [Header("Local Player Visuals")]
    public Renderer[] renderersToHideForOwner;

    [Header("Smoothing")]
    public float localPositionSmoothing = 25f;
    public float remotePositionSmoothing = 15f;
    public float rotationSmoothing = 20f;

    public int PlayerId { get; private set; } = -1;
    public int Health { get; private set; } = 100;
    public bool IsLocalPlayer { get; private set; }

    private Vector3 targetPosition;
    private float targetYaw;
    private float targetPitch;
    private bool receivedFirstState;

    public void Configure(int playerId, bool isLocalPlayer)
    {
        PlayerId = playerId;
        IsLocalPlayer = isLocalPlayer;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (audioListener == null)
        {
            audioListener = GetComponentInChildren<AudioListener>(true);
        }

        if (pitchPivot == null && playerCamera != null)
        {
            pitchPivot = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
        }

        if (IsLocalPlayer)
        {
            ActivateLocalCamera();
        }
        else
        {
            DisableRemoteCamera();
        }

        if (renderersToHideForOwner != null)
        {
            foreach (Renderer currentRenderer in renderersToHideForOwner)
            {
                if (currentRenderer != null)
                {
                    currentRenderer.enabled = !IsLocalPlayer;
                }
            }
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider currentCollider in colliders)
        {
            currentCollider.enabled = false;
        }

        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rigidbodyComponent in rigidbodies)
        {
            rigidbodyComponent.isKinematic = true;
            rigidbodyComponent.detectCollisions = false;
        }
    }

    private void ActivateLocalCamera()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Local PlayerView does not have a Camera.");
            return;
        }

        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Camera currentCamera in allCameras)
        {
            if (currentCamera == playerCamera)
            {
                continue;
            }

            currentCamera.enabled = false;

            if (currentCamera.CompareTag("MainCamera"))
            {
                currentCamera.tag = "Untagged";
            }
        }

        AudioListener[] allAudioListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioListener currentAudioListener in allAudioListeners)
        {
            currentAudioListener.enabled = currentAudioListener == audioListener;
        }

        playerCamera.enabled = true;
        playerCamera.tag = "MainCamera";

        if (audioListener != null)
        {
            audioListener.enabled = true;
        }

        Debug.Log("Player " + PlayerId + " camera activated as Main Camera.");
    }

    private void DisableRemoteCamera()
    {
        if (playerCamera != null)
        {
            playerCamera.enabled = false;

            if (playerCamera.CompareTag("MainCamera"))
            {
                playerCamera.tag = "Untagged";
            }
        }

        if (audioListener != null)
        {
            audioListener.enabled = false;
        }
    }

    public void ApplyServerState(Vector3 position, float yaw, float pitch, int health)
    {
        targetPosition = position;
        targetYaw = yaw;
        targetPitch = pitch;
        Health = health;

        if (!receivedFirstState)
        {
            receivedFirstState = true;
            transform.position = targetPosition;
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);

            if (pitchPivot != null)
            {
                pitchPivot.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
            }
        }
    }

    void Update()
    {
        if (!receivedFirstState)
        {
            return;
        }

        float positionSmoothing = IsLocalPlayer ? localPositionSmoothing : remotePositionSmoothing;
        float positionInterpolation = 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime);
        float rotationInterpolation = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPosition, positionInterpolation);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, targetYaw, 0f), rotationInterpolation);

        if (pitchPivot != null)
        {
            pitchPivot.localRotation = Quaternion.Slerp(pitchPivot.localRotation, Quaternion.Euler(targetPitch, 0f, 0f), rotationInterpolation);
        }
    }
}