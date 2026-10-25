using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.UpdateService;
using Mirror;
using UnityEngine;

public class EOSMirrorPlayerCameraController : INetworkPlayerCameraController
{
    private readonly GameInputActions _gameInputActions;
    private bool _isSetUp;

    public EOSMirrorPlayerCameraController(GameInputActions gameInputActions)
    {
        _gameInputActions = gameInputActions;
    }

}