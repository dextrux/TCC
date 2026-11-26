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
    private INetworkQuickTimeEventService _networkQuickTimeEventService;
    private LazyInject<EOSMirrorLobbyService> _lobbyService;
    private DiContainer _container;

    private bool _serverGameplayReady;
    private bool _serverLobbyGameStarted;
    private bool _clientGameplayReady;
    private bool _playerRequested;
    private bool _clientStartGamePending;

    private readonly HashSet<NetworkConnectionToClient> _pendingPlayers = new HashSet<NetworkConnectionToClient>();

    
    [Inject]
    public void Construct(INetworkSoundService networkSoundService, INetworkInventoryService networkInventoryService, INetworkQuickTimeEventService networkQuickTimeEventService, LazyInject<EOSMirrorLobbyService> lobbyService, DiContainer container)
    {
        _networkSoundService = networkSoundService;
        _networkInventoryService = networkInventoryService;
        _networkQuickTimeEventService = networkQuickTimeEventService;
        _lobbyService = lobbyService;
        _container = container;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _serverGameplayReady = false;
        _serverLobbyGameStarted = false;
        _pendingPlayers.Clear();

        NetworkServer.RegisterHandler<PlayerCameraInputMessage>(OnPlayerCameraInputMessage);
        NetworkServer.RegisterHandler<PlayerMovementInputMessage>(OnPlayerMovementInputMessage);

        _networkQuickTimeEventService?.SetUp();

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();

        LogService.Log("Mirror server started.");
    }

    public override void OnStopServer()
    {
        _serverGameplayReady = false;
        _serverLobbyGameStarted = false;
        _pendingPlayers.Clear();

        _networkSoundService?.StopServer();
        _networkInventoryService?.StopServer();

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
        _clientStartGamePending = false;

        // Substitui o registro padrão pelo spawn personalizado.
        if (playerPrefab != null)
        {
            NetworkClient.UnregisterPrefab(playerPrefab);

            NetworkClient.RegisterPrefab(
                playerPrefab,
                SpawnClientPlayer,
                UnspawnClientPlayer
            );
        }

        NetworkClient.RegisterHandler<LobbyPlayerCountMessage>(
            OnLobbyPlayerCountMessage
        );

        NetworkClient.RegisterHandler<LobbyStartGameMessage>(
            OnLobbyStartGameMessage
        );

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();
    }

    public override void OnStopClient()
    {
        _clientGameplayReady = false;
        _playerRequested = false;
        _clientStartGamePending = false;

        NetworkClient.UnregisterHandler<LobbyPlayerCountMessage>();
        NetworkClient.UnregisterHandler<LobbyStartGameMessage>();

        _networkSoundService?.StopClient();
        _networkInventoryService?.StopClient();

        _networkQuickTimeEventService?.StopClient();

        _lobbyService?.Value.HandleMirrorDisconnected();

        base.OnStopClient();
    }

    /*public override void OnServerConnect(NetworkConnectionToClient connection)
    {
        base.OnServerConnect(connection);

        BroadcastLobbyPlayerCount();

        Z esteve aqui
    }
    */

    public override void OnServerConnect(NetworkConnectionToClient connection)
    {
        base.OnServerConnect(connection);

        /*LogService.LogTopic(
            "Jogador conectado. Connection ID: " + connection.connectionId,
            LogTopicType.GameState
        );*/

        LogService.LogTopic(
            "<color=#FF00FF>JOGADOR CONECTADO!</color> Connection ID: " + connection.connectionId,
            LogTopicType.GameState
        );

        BroadcastLobbyPlayerCount();

        // Se a partida ja comecou, avisa o jogador que acabou de conectar.
        if (_serverLobbyGameStarted && connection != null)
        {
            connection.Send(new LobbyStartGameMessage());

            LogService.Log("Sending current game state to reconnected player: " + connection.connectionId);
        }
    }

    public void StartLobbyGame()
    {
        if (!NetworkServer.active || _serverLobbyGameStarted)
        {
            return;
        }

        // Registra o estado antes de enviar o comando.
        _serverLobbyGameStarted = true;

        NetworkServer.SendToAll(new LobbyStartGameMessage());

        LogService.Log("Lobby match started. Future connections will join the active game.");
    }

    public override void OnClientConnect()
    {
        // Nao chamar base.OnClientConnect().
        // O player sera criado somente depois do carregamento do gameplay.

        _networkSoundService?.SetUp();
        _networkInventoryService?.SetUp();

        _lobbyService?.Value.HandleMirrorConnected();

        // Trata o caso em que a mensagem de inicio chegou
        // antes da conclusao do callback de conexao.
        if (_clientStartGamePending && _lobbyService != null && _lobbyService.Value.IsConnected)
        {
            _clientStartGamePending = false;

            _lobbyService.Value.HandleGameStarted();
        }

        TryRequestLocalPlayer();

        LogService.Log("Mirror client connected.");
    }

    /*{
        _lobbyService?.Value.HandleMirrorDisconnected();

        base.OnClientDisconnect();
    }*/

    public override void OnClientDisconnect()
    {
        /*LogService.LogTopic(
            "Cliente desconectado do servidor.",
            LogTopicType.GameState
        );*/

        LogService.LogTopic(
            "<color=#FF00FF>CLIENTE DESCONECTADO DO SERVIDOR!</color>",
            LogTopicType.GameState
        );

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
            return;

        if (!connection.isAuthenticated || !connection.isReady)
        {
            LogService.LogWarning(
                $"[NM] SpawnPlayer ignorado: conn={connection.connectionId} " +
                $"authenticated={connection.isAuthenticated} " +
                $"ready={connection.isReady}");

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

        GameObject player = Instantiate(
            playerPrefab,
            spawnPosition,
            spawnRotation
        );

        player.name = "NetworkPlayer_" + connection.connectionId;

        if (_container != null)
        {
            _container.InjectGameObject(player);
        }
        else
        {
            LogService.LogError(
                "[NM] DiContainer nao foi injetado.");
        }

        bool added = NetworkServer.AddPlayerForConnection(
            connection,
            player
        );

        if (!added)
        {
            LogService.LogError(
                $"[NM] Falha ao associar Player à conexão {connection.connectionId}");

            return;
        }

        _networkInventoryService?.AddPlayer(connection.connectionId);

        LogService.Log(
            $"Player spawned. Connection ID: {connection.connectionId} " +
            $"| Position: {spawnPosition}");
    }
    
    private GameObject SpawnClientPlayer(SpawnMessage message)
    {
        GameObject player = Instantiate(
            playerPrefab,
            message.position,
            message.rotation
        );

        player.name = "NetworkPlayer_Client";

        if (_container != null)
        {
            _container.InjectGameObject(player);

            LogService.Log(
                "[NM] Player do cliente recebeu injecao Zenject."
            );
        }
        else
        {
            LogService.LogError(
                "[NM] Container nulo ao criar Player no cliente."
            );
        }

        return player;
    }

    private void UnspawnClientPlayer(GameObject player)
    {
        Destroy(player);
    }

    public void NotifyGameplayReady()
    {
        // DIAGNOSTICO: se este log nunca aparece, ninguem esta avisando que a gameplay carregou
        LogService.Log($"[NM] NotifyGameplayReady | serverActive={NetworkServer.active} pendentes={_pendingPlayers.Count}");

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
            return;

        NetworkConnectionToClient[] pending =
            new NetworkConnectionToClient[_pendingPlayers.Count];

        _pendingPlayers.CopyTo(pending);
        _pendingPlayers.Clear();

        foreach (NetworkConnectionToClient connection in pending)
        {
            if (connection != null &&
                connection.isAuthenticated &&
                connection.isReady &&
                connection.identity == null)
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
        // Nao descartar a mensagem caso o lobby ainda nao
        // tenha terminado o processo de conexao.
        if (_lobbyService == null || !_lobbyService.Value.IsConnected)
        {
            _clientStartGamePending = true;
            return;
        }

        _clientStartGamePending = false;

        _lobbyService.Value.HandleGameStarted();
    }

    /*public override void OnServerDisconnect(NetworkConnectionToClient connection)
    {
        _pendingPlayers.Remove(connection);

        _networkInventoryService?.RemovePlayer(connection.connectionId);

        LogService.Log("Player disconnected. Connection ID: " + connection.connectionId);

        base.OnServerDisconnect(connection);

        BroadcastLobbyPlayerCount(connection);
    }
    */

    public override void OnServerDisconnect(NetworkConnectionToClient connection)
    {
        _pendingPlayers.Remove(connection);

        _networkInventoryService?.RemovePlayer(connection.connectionId);

        LogService.LogTopic(
            "<color=#FF00FF>JOGADOR DESCONECTADO!</color> Connection ID: " + connection.connectionId,
            LogTopicType.GameState
        );

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