using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NetworkTcpClient = System.Net.Sockets.TcpClient;

public class TcpServer : MonoBehaviour
{
    public static TcpServer Instance { get; private set; }

    [Header("Server")]
    public int maxPlayers = 8;
    public int simulationRate = 60;
    public int stateSendRate = 30;

    [Header("Server Player")]
    public GameObject serverPlayerPrefab;
    public Transform[] spawnPoints;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.4f;
    public float gravity = -20f;
    public float lookSensitivity = 2.2f;

    [Header("Shooting")]
    public float eyeHeight = 1.6f;
    public float shotDistance = 100f;
    public int shotDamage = 25;
    public float fireCooldown = 0.15f;
    public LayerMask hitMask = ~0;

    public bool IsRunning
    {
        get
        {
            return running;
        }
    }

    public string Status { get; private set; } = "Servidor desligado";

    private enum ServerEventType
    {
        Connected,
        Message,
        Disconnected
    }

    private struct ServerEvent
    {
        public ServerEventType Type;
        public int PlayerId;
        public string Message;

        public ServerEvent(ServerEventType type, int playerId, string message)
        {
            Type = type;
            PlayerId = playerId;
            Message = message;
        }
    }

    private readonly List<TcpServerConnection> connections = new List<TcpServerConnection>();
    private readonly Dictionary<int, ServerPlayerState> players = new Dictionary<int, ServerPlayerState>();

    private readonly Queue<ServerEvent> serverEvents = new Queue<ServerEvent>();
    private readonly object serverEventsLock = new object();
    private readonly object connectionsLock = new object();

    private TcpListener listener;
    private Thread acceptThread;

    private volatile bool running;

    private int nextPlayerId;
    private long serverTick;

    private float simulationAccumulator;
    private float stateAccumulator;

    void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
    }

    public bool StartServer(string bindIp, int port)
    {
        if (running)
        {
            return true;
        }

        if (!ValidateConfiguration())
        {
            return false;
        }

        if (port < 1 || port > 65535)
        {
            Status = "Porta inválida.";
            return false;
        }

        IPAddress address;

        if (string.IsNullOrWhiteSpace(bindIp) || bindIp.Trim() == "0.0.0.0")
        {
            address = IPAddress.Any;
        }
        else if (!IPAddress.TryParse(bindIp.Trim(), out address))
        {
            Status = "IP inválido.";
            return false;
        }

        try
        {
            nextPlayerId = 0;
            serverTick = 0;
            simulationAccumulator = 0f;
            stateAccumulator = 0f;

            listener = new TcpListener(address, port);
            listener.Start();

            running = true;
            Status = "Servidor ativo em " + bindIp + ":" + port;

            acceptThread = new Thread(AcceptLoop);
            acceptThread.IsBackground = true;
            acceptThread.Start();

            Debug.Log("TCP server started on " + bindIp + ":" + port);
            return true;
        }
        catch (Exception exception)
        {
            running = false;
            Status = "Erro ao iniciar servidor: " + exception.Message;
            Debug.LogError("Failed to start TCP server: " + exception.Message);
            return false;
        }
    }

    private bool ValidateConfiguration()
    {
        if (serverPlayerPrefab == null)
        {
            Status = "Server Player Prefab não configurado.";
            Debug.LogError("Server Player Prefab is not assigned.");
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Status = "Nenhum Spawn Point configurado.";
            Debug.LogError("No Spawn Points are assigned.");
            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Status = "Existe um Spawn Point vazio.";
                Debug.LogError("Spawn Point " + i + " is null.");
                return false;
            }
        }

        return true;
    }

    private void AcceptLoop()
    {
        while (running)
        {
            NetworkTcpClient socket;

            try
            {
                socket = listener.AcceptTcpClient();
            }
            catch
            {
                if (!running)
                {
                    break;
                }

                continue;
            }

            if (GetConnectedCount() >= maxPlayers)
            {
                RejectConnection(socket);
                continue;
            }

            int playerId = nextPlayerId++;
            TcpServerConnection connection;

            try
            {
                connection = new TcpServerConnection(playerId, socket);
            }
            catch
            {
                socket.Close();
                continue;
            }

            lock (connectionsLock)
            {
                connections.Add(connection);
            }

            EnqueueServerEvent(ServerEventType.Connected, playerId, null);
            connection.Start(OnConnectionMessage, OnConnectionDisconnected);
        }
    }

    private int GetConnectedCount()
    {
        int count = 0;

        lock (connectionsLock)
        {
            foreach (TcpServerConnection connection in connections)
            {
                if (connection.IsConnected)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void RejectConnection(NetworkTcpClient socket)
    {
        try
        {
            TcpServerConnection connection = new TcpServerConnection(-1, socket);
            connection.Send(NetworkProtocol.CreateFull("Servidor cheio"));
            connection.Close(false);
        }
        catch
        {
            try
            {
                socket.Close();
            }
            catch
            {
            }
        }
    }

    private void OnConnectionMessage(int playerId, string message)
    {
        EnqueueServerEvent(ServerEventType.Message, playerId, message);
    }

    private void OnConnectionDisconnected(int playerId)
    {
        EnqueueServerEvent(ServerEventType.Disconnected, playerId, null);
    }

    private void EnqueueServerEvent(ServerEventType type, int playerId, string message)
    {
        lock (serverEventsLock)
        {
            serverEvents.Enqueue(new ServerEvent(type, playerId, message));
        }
    }

    void Update()
    {
        if (!running)
        {
            return;
        }

        ProcessServerEvents();
        UpdateSimulation();
        UpdateStateBroadcast();
    }

    private void ProcessServerEvents()
    {
        while (true)
        {
            ServerEvent serverEvent;

            lock (serverEventsLock)
            {
                if (serverEvents.Count == 0)
                {
                    break;
                }

                serverEvent = serverEvents.Dequeue();
            }

            switch (serverEvent.Type)
            {
                case ServerEventType.Connected:
                {
                    HandleConnected(serverEvent.PlayerId);
                    break;
                }

                case ServerEventType.Message:
                {
                    ProcessClientMessage(serverEvent.PlayerId, serverEvent.Message);
                    break;
                }

                case ServerEventType.Disconnected:
                {
                    RemovePlayer(serverEvent.PlayerId);
                    break;
                }
            }
        }
    }

    private void HandleConnected(int playerId)
    {
        if (!CreatePlayer(playerId))
        {
            DisconnectClient(playerId);
            return;
        }

        TcpServerConnection connection = GetConnection(playerId);

        if (connection != null)
        {
            Debug.Log("Player " + playerId + " connected from " + connection.RemoteEndpoint);
        }

        SendTo(playerId, NetworkProtocol.CreateWelcome(playerId));
        Broadcast(NetworkProtocol.CreateJoined(playerId));
        BroadcastPlayerCount();
    }

    private void ProcessClientMessage(int playerId, string message)
    {
        string[] parts = NetworkProtocol.Split(message);

        if (parts.Length == 0)
        {
            return;
        }

        switch (parts[0])
        {
            case NetworkProtocol.Hello:
            {
                HandleHello(playerId, parts);
                break;
            }

            case NetworkProtocol.Input:
            {
                HandleInput(playerId, parts);
                break;
            }

            case NetworkProtocol.Ping:
            {
                SendTo(playerId, NetworkProtocol.CreatePong(parts.Length > 1 ? parts[1] : "0"));
                break;
            }

            case NetworkProtocol.Bye:
            {
                DisconnectClient(playerId);
                break;
            }
        }
    }

    private void HandleHello(int playerId, string[] parts)
    {
        if (parts.Length < 2)
        {
            return;
        }

        ServerPlayerState player;

        if (players.TryGetValue(playerId, out player))
        {
            player.Name = parts[1];
        }
    }

    private void HandleInput(int playerId, string[] parts)
    {
        ServerPlayerState player;

        if (!players.TryGetValue(playerId, out player))
        {
            return;
        }

        PlayerInputMessage input;

        if (!NetworkProtocol.TryParseInput(parts, out input))
        {
            return;
        }

        if (input.Sequence <= player.LastInputSequence)
        {
            return;
        }

        player.LastInputSequence = input.Sequence;
        player.MoveX = Mathf.Clamp(input.MoveX, -1f, 1f);
        player.MoveZ = Mathf.Clamp(input.MoveZ, -1f, 1f);
        player.PendingLookX += Mathf.Clamp(input.LookX, -100f, 100f);
        player.PendingLookY += Mathf.Clamp(input.LookY, -100f, 100f);

        if (input.Jump)
        {
            player.PendingJump = true;
        }

        if (input.Fire)
        {
            player.PendingFire = true;
        }
    }

    private bool CreatePlayer(int playerId)
    {
        if (players.ContainsKey(playerId))
        {
            return true;
        }

        Transform spawnPoint = GetSpawnPoint(playerId);

        if (spawnPoint == null)
        {
            Debug.LogError("No valid Spawn Point found for player " + playerId + ".");
            return false;
        }

        GameObject playerObject = Instantiate(serverPlayerPrefab, spawnPoint.position, spawnPoint.rotation);
        playerObject.name = "ServerPlayer_" + playerId;

        CharacterController controller = playerObject.GetComponent<CharacterController>();

        if (controller == null)
        {
            controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
        }

        controller.enabled = false;
        playerObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        controller.enabled = true;

        FpsServerPlayerBody body = playerObject.GetComponent<FpsServerPlayerBody>();

        if (body == null)
        {
            body = playerObject.AddComponent<FpsServerPlayerBody>();
        }

        body.PlayerId = playerId;

        DisableServerVisuals(playerObject);

        ServerPlayerState player = new ServerPlayerState();
        player.Id = playerId;
        player.Object = playerObject;
        player.Controller = controller;
        player.Body = body;
        player.Yaw = spawnPoint.eulerAngles.y;
        player.Pitch = 0f;

        players.Add(playerId, player);

        Debug.Log("Player " + playerId + " spawned at " + spawnPoint.name + " | Position: " + spawnPoint.position);
        return true;
    }

    private void DisableServerVisuals(GameObject playerObject)
    {
        Renderer[] renderers = playerObject.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer currentRenderer in renderers)
        {
            currentRenderer.enabled = false;
        }

        Camera[] cameras = playerObject.GetComponentsInChildren<Camera>(true);

        foreach (Camera currentCamera in cameras)
        {
            currentCamera.enabled = false;
        }

        AudioListener[] audioListeners = playerObject.GetComponentsInChildren<AudioListener>(true);

        foreach (AudioListener currentAudioListener in audioListeners)
        {
            currentAudioListener.enabled = false;
        }
    }

    private Transform GetSpawnPoint(int playerId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        return spawnPoints[playerId % spawnPoints.Length];
    }

    private void UpdateSimulation()
    {
        float simulationStep = 1f / Mathf.Max(1, simulationRate);
        simulationAccumulator += Time.unscaledDeltaTime;

        while (simulationAccumulator >= simulationStep)
        {
            Simulate(simulationStep);
            simulationAccumulator -= simulationStep;
        }
    }

    private void Simulate(float deltaTime)
    {
        serverTick++;

        foreach (ServerPlayerState player in players.Values)
        {
            SimulatePlayer(player, deltaTime);
        }
    }

    private void SimulatePlayer(ServerPlayerState player, float deltaTime)
    {
        if (player.Controller == null || !player.Controller.enabled)
        {
            return;
        }

        player.Yaw += player.PendingLookX * lookSensitivity;
        player.Pitch -= player.PendingLookY * lookSensitivity;
        player.Pitch = Mathf.Clamp(player.Pitch, -85f, 85f);

        player.PendingLookX = 0f;
        player.PendingLookY = 0f;

        player.Object.transform.rotation = Quaternion.Euler(0f, player.Yaw, 0f);

        Vector3 localMovement = new Vector3(player.MoveX, 0f, player.MoveZ);
        localMovement = Vector3.ClampMagnitude(localMovement, 1f);

        Vector3 worldMovement = player.Object.transform.TransformDirection(localMovement);

        if (player.Controller.isGrounded && player.VerticalVelocity < 0f)
        {
            player.VerticalVelocity = -2f;
        }

        if (player.PendingJump && player.Controller.isGrounded)
        {
            player.VerticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        player.PendingJump = false;
        player.VerticalVelocity += gravity * deltaTime;

        Vector3 velocity = worldMovement * moveSpeed;
        velocity.y = player.VerticalVelocity;

        player.Controller.Move(velocity * deltaTime);

        if (player.PendingFire)
        {
            player.PendingFire = false;
            TryShoot(player);
        }
    }

    private void TryShoot(ServerPlayerState shooter)
    {
        if (Time.unscaledTime - shooter.LastFireTime < fireCooldown)
        {
            return;
        }

        shooter.LastFireTime = Time.unscaledTime;

        Vector3 direction = Quaternion.Euler(shooter.Pitch, shooter.Yaw, 0f) * Vector3.forward;
        Vector3 origin = shooter.Object.transform.position + Vector3.up * eyeHeight + direction * 0.2f;

        RaycastHit hit;

        if (!Physics.Raycast(origin, direction, out hit, shotDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        FpsServerPlayerBody body = hit.collider.GetComponentInParent<FpsServerPlayerBody>();

        if (body == null || body.PlayerId == shooter.Id)
        {
            return;
        }

        ServerPlayerState victim;

        if (!players.TryGetValue(body.PlayerId, out victim))
        {
            return;
        }

        ApplyDamage(shooter, victim);
    }

    private void ApplyDamage(ServerPlayerState attacker, ServerPlayerState victim)
    {
        victim.Health = Mathf.Max(0, victim.Health - shotDamage);

        Broadcast(NetworkProtocol.CreateHit(attacker.Id, victim.Id, victim.Health));

        if (victim.Health > 0)
        {
            return;
        }

        Broadcast(NetworkProtocol.CreateDied(attacker.Id, victim.Id));
        Respawn(victim);
    }

    private void Respawn(ServerPlayerState player)
    {
        Transform spawnPoint = GetSpawnPoint(player.Id);

        if (spawnPoint == null)
        {
            return;
        }

        player.Controller.enabled = false;
        player.Object.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        player.Controller.enabled = true;

        player.Yaw = spawnPoint.eulerAngles.y;
        player.Pitch = 0f;
        player.VerticalVelocity = 0f;
        player.Health = 100;
        player.MoveX = 0f;
        player.MoveZ = 0f;
        player.PendingLookX = 0f;
        player.PendingLookY = 0f;
        player.PendingJump = false;
        player.PendingFire = false;
    }

    private void UpdateStateBroadcast()
    {
        float stateStep = 1f / Mathf.Max(1, stateSendRate);
        stateAccumulator += Time.unscaledDeltaTime;

        while (stateAccumulator >= stateStep)
        {
            SendStates();
            stateAccumulator -= stateStep;
        }
    }

    private void SendStates()
    {
        foreach (ServerPlayerState player in players.Values)
        {
            if (player.Object == null)
            {
                continue;
            }

            Vector3 position = player.Object.transform.position;
            Broadcast(NetworkProtocol.CreateState(serverTick, player.Id, position.x, position.y, position.z, player.Yaw, player.Pitch, player.Health));
        }
    }

    private void BroadcastPlayerCount()
    {
        Broadcast(NetworkProtocol.CreatePlayerCount(players.Count));
    }

    private TcpServerConnection GetConnection(int playerId)
    {
        lock (connectionsLock)
        {
            foreach (TcpServerConnection connection in connections)
            {
                if (connection.Id == playerId)
                {
                    return connection;
                }
            }
        }

        return null;
    }

    private void SendTo(int playerId, string message)
    {
        TcpServerConnection connection = GetConnection(playerId);

        if (connection != null)
        {
            connection.Send(message);
        }
    }

    private void Broadcast(string message)
    {
        List<TcpServerConnection> snapshot;

        lock (connectionsLock)
        {
            snapshot = new List<TcpServerConnection>(connections);
        }

        foreach (TcpServerConnection connection in snapshot)
        {
            if (connection.IsConnected)
            {
                connection.Send(message);
            }
        }
    }

    private void DisconnectClient(int playerId)
    {
        TcpServerConnection connection = GetConnection(playerId);

        if (connection != null)
        {
            connection.Close(true);
        }
    }

    private void RemovePlayer(int playerId)
    {
        bool removedSomething = false;

        ServerPlayerState player;

        if (players.TryGetValue(playerId, out player))
        {
            if (player.Object != null)
            {
                Destroy(player.Object);
            }

            players.Remove(playerId);
            Broadcast(NetworkProtocol.CreateDespawn(playerId));
            removedSomething = true;
        }

        lock (connectionsLock)
        {
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                if (connections[i].Id != playerId)
                {
                    continue;
                }

                connections[i].Close(false);
                connections.RemoveAt(i);
                removedSomething = true;
            }
        }

        if (!removedSomething)
        {
            return;
        }

        BroadcastPlayerCount();
        Debug.Log("Player " + playerId + " disconnected.");
    }

    public void StopServer()
    {
        if (!running)
        {
            return;
        }

        running = false;

        try
        {
            listener.Stop();
        }
        catch
        {
        }

        lock (connectionsLock)
        {
            foreach (TcpServerConnection connection in connections)
            {
                connection.Close(false);
            }

            connections.Clear();
        }

        foreach (ServerPlayerState player in players.Values)
        {
            if (player.Object != null)
            {
                Destroy(player.Object);
            }
        }

        players.Clear();

        lock (serverEventsLock)
        {
            serverEvents.Clear();
        }

        Status = "Servidor desligado";
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    void OnDestroy()
    {
        StopServer();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
