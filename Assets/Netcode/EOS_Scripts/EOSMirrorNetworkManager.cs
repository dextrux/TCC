
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using UnityEngine;
using Zenject;

public class EOSMirrorNetworkManager : NetworkManager
{
    private INetworkSoundService _networkSoundService;
    private INetworkInventoryService _networkInventoryService;
    private DiContainer _container;

    [Inject]
    public void Construct(INetworkSoundService networkSoundService, INetworkInventoryService networkInventoryService, DiContainer container)
    {
        _networkSoundService = networkSoundService;
        _networkInventoryService = networkInventoryService;
        _container = container;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<PlayerCameraInputMessage>(OnPlayerCameraInputMessage);
        NetworkServer.RegisterHandler<PlayerMovementInputMessage>(OnPlayerMovementInputMessage);

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();

        LogService.Log("Mirror server started.");
    }


    public override void OnStopServer()
    {
        _networkSoundService?.StopServer();
        _networkInventoryService?.StopServer();

        NetworkServer.UnregisterHandler<PlayerCameraInputMessage>();
        NetworkServer.UnregisterHandler<PlayerMovementInputMessage>();

        base.OnStopServer();
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        LogService.Log("Mirror host started.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();
    }

    public override void OnStopClient()
    {
        _networkSoundService?.StopClient();
        _networkInventoryService?.StopClient();

        base.OnStopClient();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();

        LogService.Log("Mirror client connected.");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient connection)
    {
        if (playerPrefab == null)
        {
            LogService.LogError("Player Prefab is not assigned.");
            connection.Disconnect();
            return;
        }

        Transform startPosition = GetStartPosition();

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (startPosition != null)
        {
            spawnPosition = startPosition.position;
            spawnRotation = startPosition.rotation;
        }

        GameObject player = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        player.name = "NetworkPlayer_" + connection.connectionId;
        _container.InjectGameObject(player); //Caso nao tenha isso o inventario acusa como nao injetado (nao me pergunte pq)

        NetworkServer.AddPlayerForConnection(connection, player);

        LogService.Log("Player spawned. Connection ID: " + connection.connectionId + " | Position: " + spawnPosition);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient connection)
    {
        _networkInventoryService?.RemovePlayer(connection.connectionId);
        LogService.Log("Player disconnected. Connection ID: " + connection.connectionId);
        base.OnServerDisconnect(connection);
    }

    private void OnPlayerCameraInputMessage(NetworkConnectionToClient connection, PlayerCameraInputMessage message)
    {
        if (connection.identity == null)
        {
            return;
        }

        MirrorPlayerCamera playerCamera = connection.identity.GetComponent<MirrorPlayerCamera>();

        if (playerCamera == null)
        {
            LogService.LogWarning("MirrorPlayerCamera was not found on the connected player.");
            return;
        }

        playerCamera.ServerApplyCameraInput(message.CameraInput);
    }

    private void OnPlayerMovementInputMessage(NetworkConnectionToClient connection, PlayerMovementInputMessage message) {
        if (connection.identity == null) {
            return;
        }

        MirrorPlayerCamera playerCamera = connection.identity.GetComponent<MirrorPlayerCamera>();

        if (playerCamera == null) {
            LogService.LogWarning("MirrorPlayerCamera was not found on the connected player.");
            return;
        }

        playerCamera.ServerApplyMovementInput(message.MovementInput);
    }
}
