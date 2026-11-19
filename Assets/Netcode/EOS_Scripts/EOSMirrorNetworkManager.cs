
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class EOSMirrorNetworkManager : NetworkManager
{
    private INetworkSoundService _networkSoundService;
    private INetworkInventoryService _networkInventoryService;
    private LazyInject<EOSMirrorLobbyService> _lobbyService;
    private DiContainer _container;

    private bool _serverGameplayReady;
    private bool _clientGameplayReady;
    private bool _playerRequested;

    private readonly HashSet<NetworkConnectionToClient> _pendingPlayers = new HashSet<NetworkConnectionToClient>();

    private INetworkQuickTimeEventService _networkQuickTimeEventService;
    [Inject]
    public void Construct(INetworkSoundService networkSoundService, INetworkInventoryService networkInventoryService, LazyInject<EOSMirrorLobbyService> lobbyService, DiContainer container)
    {
        _networkSoundService = networkSoundService;
        _networkInventoryService = networkInventoryService;
        _lobbyService = lobbyService;
        _container = container;
    }
    [Inject]
    public void Construct(
        INetworkSoundService networkSoundService,
        INetworkInventoryService networkInventoryService,
        INetworkQuickTimeEventService networkQuickTimeEventService,
        DiContainer container)
    {
        _networkSoundService = networkSoundService;
        _networkInventoryService = networkInventoryService;
        _networkQuickTimeEventService = networkQuickTimeEventService;
        _container = container;
    }
    public override void OnStartServer()
    {
        base.OnStartServer();

        _serverGameplayReady = false;
        _pendingPlayers.Clear();

        NetworkServer.RegisterHandler<PlayerCameraInputMessage>(OnPlayerCameraInputMessage);
        NetworkServer.RegisterHandler<PlayerMovementInputMessage>(OnPlayerMovementInputMessage);

        //z
        _networkQuickTimeEventService?.SetUp();

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();

        LogService.Log("Mirror server started.");
    }

    public override void OnStopServer()
    {
        _serverGameplayReady = false;
        _pendingPlayers.Clear();

        _networkSoundService?.StopServer();
        _networkInventoryService?.StopServer();

        //z
        _networkQuickTimeEventService?.StopServer();

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

        _clientGameplayReady = false;
        _playerRequested = false;

        NetworkClient.RegisterHandler<LobbyPlayerCountMessage>(OnLobbyPlayerCountMessage);
        NetworkClient.RegisterHandler<LobbyStartGameMessage>(OnLobbyStartGameMessage);

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();
    }

    public override void OnStopClient()
    {
        _clientGameplayReady = false;
        _playerRequested = false;

        NetworkClient.UnregisterHandler<LobbyPlayerCountMessage>();
        NetworkClient.UnregisterHandler<LobbyStartGameMessage>();

        _networkSoundService?.StopClient();
        _networkInventoryService?.StopClient();

        _networkQuickTimeEventService?.StopClient();

        _lobbyService?.Value.HandleMirrorDisconnected();

        base.OnStopClient();
    }

    public override void OnServerConnect(NetworkConnectionToClient connection)
    {
        base.OnServerConnect(connection);

        BroadcastLobbyPlayerCount();
    }

    public override void OnClientConnect()
    {
        // Nao chamar base.OnClientConnect().
        // O player sera criado somente depois do carregamento do gameplay.

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();

        _lobbyService?.Value.HandleMirrorConnected();

        TryRequestLocalPlayer();

        LogService.Log("Mirror client connected.");
    }

    public override void OnClientDisconnect()
    {
        _lobbyService?.Value.HandleMirrorDisconnected();

        base.OnClientDisconnect();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient connection)
    {
        if (connection.identity != null)
        {
            LogService.LogWarning("Connection already has a player: " + connection.connectionId);
            return;
        }

        if (!_serverGameplayReady)
        {
            _pendingPlayers.Add(connection);

            LogService.Log("Player waiting for gameplay: " + connection.connectionId);
            return;
        }

        SpawnPlayer(connection);
    }

    private void SpawnPlayer(NetworkConnectionToClient connection)
    {
        if (connection == null || connection.identity != null)
        {
            return;
        }

        if (!connection.isAuthenticated || !connection.isReady)
        {
            return;
        }

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

        _container.InjectGameObject(player);

        NetworkServer.AddPlayerForConnection(connection, player);

        LogService.Log("Player spawned. Connection ID: " + connection.connectionId + " | Position: " + spawnPosition);
    }

    public void NotifyGameplayReady()
    {
        if (NetworkServer.active)
        {
            _serverGameplayReady = true;

            ProcessPendingPlayers();
        }

        _clientGameplayReady = true;

        TryRequestLocalPlayer();
    }

    private void TryRequestLocalPlayer()
    {
        if (!_clientGameplayReady || _playerRequested)
        {
            return;
        }

        if (!NetworkClient.isConnected)
        {
            return;
        }

        if (NetworkClient.localPlayer != null)
        {
            _playerRequested = true;
            return;
        }

        if (!NetworkClient.ready)
        {
            NetworkClient.Ready();
        }

        _playerRequested = true;

        NetworkClient.AddPlayer();
    }

    private void ProcessPendingPlayers()
    {
        if (!_serverGameplayReady)
        {
            return;
        }

        NetworkConnectionToClient[] pending = new NetworkConnectionToClient[_pendingPlayers.Count];

        _pendingPlayers.CopyTo(pending);
        _pendingPlayers.Clear();

        foreach (NetworkConnectionToClient connection in pending)
        {
            if (connection != null && connection.isAuthenticated && connection.isReady && connection.identity == null)
            {
                SpawnPlayer(connection);
            }
        }
    }

    public int GetConnectedPlayerCount()
    {
        return GetConnectedPlayerCount(null);
    }

    private int GetConnectedPlayerCount(NetworkConnectionToClient excludedConnection)
    {
        if (!NetworkServer.active)
        {
            return 0;
        }

        int count = 0;

        foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
        {
            if (connection == null || connection == excludedConnection)
            {
                continue;
            }

            if (connection.isAuthenticated)
            {
                count++;
            }
        }

        return count;
    }

    public void BroadcastLobbyPlayerCount()
    {
        BroadcastLobbyPlayerCount(null);
    }

    private void BroadcastLobbyPlayerCount(NetworkConnectionToClient excludedConnection)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        int count = GetConnectedPlayerCount(excludedConnection);

        NetworkServer.SendToAll(new LobbyPlayerCountMessage
        {
            PlayerCount = count
        });
    }

    private void OnLobbyPlayerCountMessage(LobbyPlayerCountMessage message)
    {
        _lobbyService?.Value.HandlePlayerCount(message.PlayerCount);
    }

    private void OnLobbyStartGameMessage(LobbyStartGameMessage message)
    {
        _lobbyService?.Value.HandleGameStarted();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient connection)
    {
        _pendingPlayers.Remove(connection);

        _networkInventoryService?.RemovePlayer(connection.connectionId);

        LogService.Log("Player disconnected. Connection ID: " + connection.connectionId);

        base.OnServerDisconnect(connection);

        BroadcastLobbyPlayerCount(connection);
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

    private void OnPlayerMovementInputMessage(NetworkConnectionToClient connection, PlayerMovementInputMessage message)
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

        playerCamera.ServerApplyMovementInput(message.MovementInput);
    }
}
