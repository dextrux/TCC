using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.UpdateService;
using Mirror;
using UnityEngine;

public class EOSMirrorPlayerCameraController : INetworkPlayerCameraController, IUpdatable
{
    private readonly GameInputActions _gameInputActions;
    private readonly IUpdateSubscriptionService _updateSubscriptionService;

    private bool _isSetUp;

    public EOSMirrorPlayerCameraController(GameInputActions gameInputActions, IUpdateSubscriptionService updateSubscriptionService)
    {
        _gameInputActions = gameInputActions;
        _updateSubscriptionService = updateSubscriptionService;
    }

    public void SetUp()
    {
        if (_isSetUp)
        {
            return;
        }

        _isSetUp = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _updateSubscriptionService.RegisterUpdatable(this);
    }

    public void Dispose()
    {
        if (!_isSetUp)
        {
            return;
        }

        _isSetUp = false;

        _updateSubscriptionService.UnregisterUpdatable(this);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ManagedUpdate()
    {
        if (!NetworkClient.active)
        {
            return;
        }

        if (NetworkClient.localPlayer == null)
        {
            return;
        }

        Vector2 cameraInput = _gameInputActions.Player.Look.ReadValue<Vector2>();

        if (cameraInput.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        PlayerCameraInputMessage message = new PlayerCameraInputMessage
        {
            CameraInput = cameraInput
        };

        NetworkClient.Send(message, Channels.Unreliable);
    }
}