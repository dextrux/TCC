using System.Collections.Generic;
using UnityEngine;

public class TcpClient : MonoBehaviour
{
    public static TcpClient Instance { get; private set; }

    [Header("Visual")]
    public GameObject playerViewPrefab;

    public bool Connected { get; private set; }
    public int LocalPlayerId { get; private set; } = -1;
    public int PlayerCount { get; private set; }
    public string Message { get; private set; } = "Desconectado";

    private TcpClientTransport transport;

    private readonly Dictionary<int, PlayerView> playerViews = new Dictionary<int, PlayerView>();

    void Awake()
    {
        Instance = this;
        transport = new TcpClientTransport();
        Application.runInBackground = true;
    }

    public void Connect(string ip, int port)
    {
        if (Connected)
        {
            return;
        }

        if (port < 1 || port > 65535)
        {
            Message = "Porta inválida.";
            return;
        }

        ClearViews();

        LocalPlayerId = -1;
        PlayerCount = 0;

        if (!transport.Connect(ip.Trim(), port))
        {
            Connected = false;
            Message = "Falha ao conectar: " + transport.LastError;
            Debug.LogError("TCP connection failed: " + transport.LastError);
            return;
        }

        Connected = true;
        Message = "Conectado. Aguardando servidor...";

        Send(NetworkProtocol.CreateHello(SystemInfo.deviceName));

        Debug.Log("Connected to " + ip + ":" + port);
    }

    void Update()
    {
        string message;

        while (transport.TryDequeue(out message))
        {
            ProcessMessage(message);
        }

        if (Connected && !transport.IsRunning)
        {
            HandleConnectionLost();
        }
    }

    private void ProcessMessage(string message)
    {
        string[] parts = NetworkProtocol.Split(message);

        if (parts.Length == 0)
        {
            return;
        }

        switch (parts[0])
        {
            case NetworkProtocol.Welcome:
            {
                HandleWelcome(parts);
                break;
            }

            case NetworkProtocol.PlayerCount:
            {
                HandlePlayerCount(parts);
                break;
            }

            case NetworkProtocol.State:
            {
                HandleState(parts);
                break;
            }

            case NetworkProtocol.Joined:
            {
                HandleJoined(parts);
                break;
            }

            case NetworkProtocol.Despawn:
            {
                HandleDespawn(parts);
                break;
            }

            case NetworkProtocol.Hit:
            {
                HandleHit(parts);
                break;
            }

            case NetworkProtocol.Died:
            {
                HandleDied(parts);
                break;
            }

            case NetworkProtocol.Full:
            {
                HandleFull(parts);
                break;
            }
        }
    }

    private void HandleWelcome(string[] parts)
    {
        if (parts.Length < 2)
        {
            return;
        }

        int playerId;

        if (!int.TryParse(parts[1], out playerId))
        {
            return;
        }

        LocalPlayerId = playerId;
        Message = "Você é o jogador " + playerId;
    }

    private void HandlePlayerCount(string[] parts)
    {
        if (parts.Length < 2)
        {
            return;
        }

        int playerCount;

        if (int.TryParse(parts[1], out playerCount))
        {
            PlayerCount = playerCount;
        }
    }

    private void HandleState(string[] parts)
    {
        PlayerStateMessage state;

        if (!NetworkProtocol.TryParseState(parts, out state))
        {
            return;
        }

        PlayerView view = GetOrCreateView(state.PlayerId);

        if (view == null)
        {
            return;
        }

        view.ApplyServerState(new Vector3(state.PositionX, state.PositionY, state.PositionZ), state.Yaw, state.Pitch, state.Health);
    }

    private void HandleJoined(string[] parts)
    {
        if (parts.Length >= 2)
        {
            Message = "Jogador " + parts[1] + " entrou";
        }
    }

    private void HandleDespawn(string[] parts)
    {
        if (parts.Length < 2)
        {
            return;
        }

        int playerId;

        if (int.TryParse(parts[1], out playerId))
        {
            RemoveView(playerId);
        }
    }

    private void HandleHit(string[] parts)
    {
        if (parts.Length < 4)
        {
            return;
        }

        int attackerId;
        int victimId;
        int health;

        if (!int.TryParse(parts[1], out attackerId) || !int.TryParse(parts[2], out victimId) || !int.TryParse(parts[3], out health))
        {
            return;
        }

        if (victimId == LocalPlayerId)
        {
            Message = "Você foi atingido. Vida: " + health;
        }
        else if (attackerId == LocalPlayerId)
        {
            Message = "Você acertou o jogador " + victimId;
        }
    }

    private void HandleDied(string[] parts)
    {
        if (parts.Length < 3)
        {
            return;
        }

        int attackerId;
        int victimId;

        if (!int.TryParse(parts[1], out attackerId) || !int.TryParse(parts[2], out victimId))
        {
            return;
        }

        if (victimId == LocalPlayerId)
        {
            Message = "Você morreu.";
        }
        else if (attackerId == LocalPlayerId)
        {
            Message = "Você eliminou o jogador " + victimId;
        }
    }

    private void HandleFull(string[] parts)
    {
        string fullMessage = parts.Length > 1 ? parts[1] : "Servidor cheio";
        Disconnect();
        Message = fullMessage;
    }

    private PlayerView GetOrCreateView(int playerId)
    {
        PlayerView existingView;

        if (playerViews.TryGetValue(playerId, out existingView))
        {
            return existingView;
        }

        if (playerViewPrefab == null)
        {
            Debug.LogError("Player View Prefab is not assigned.");
            return null;
        }

        GameObject viewObject = Instantiate(playerViewPrefab);
        viewObject.name = "PlayerView_" + playerId;

        PlayerView view = viewObject.GetComponent<PlayerView>();

        if (view == null)
        {
            Debug.LogError("Player View Prefab does not contain PlayerView.");
            Destroy(viewObject);
            return null;
        }

        view.Configure(playerId, playerId == LocalPlayerId);
        playerViews.Add(playerId, view);

        return view;
    }

    private void RemoveView(int playerId)
    {
        PlayerView view;

        if (!playerViews.TryGetValue(playerId, out view))
        {
            return;
        }

        if (view != null)
        {
            Destroy(view.gameObject);
        }

        playerViews.Remove(playerId);
    }

    private void ClearViews()
    {
        foreach (PlayerView view in playerViews.Values)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        playerViews.Clear();
    }

    public void SendInput(int sequence, float moveX, float moveZ, float lookX, float lookY, bool jump, bool fire)
    {
        Send(NetworkProtocol.CreateInput(sequence, moveX, moveZ, lookX, lookY, jump, fire));
    }

    public void Send(string message)
    {
        if (!Connected)
        {
            return;
        }

        if (!transport.Send(message))
        {
            HandleConnectionLost();
        }
    }

    public void Disconnect()
    {
        if (!Connected)
        {
            transport.Disconnect();
            return;
        }

        transport.Send(NetworkProtocol.CreateBye());
        transport.Disconnect();

        Connected = false;
        LocalPlayerId = -1;
        PlayerCount = 0;

        ClearViews();

        Message = "Desconectado";
    }

    private void HandleConnectionLost()
    {
        Connected = false;
        LocalPlayerId = -1;
        PlayerCount = 0;

        transport.Disconnect();
        ClearViews();

        Message = string.IsNullOrEmpty(transport.LastError) ? "Conexão perdida" : "Conexão perdida: " + transport.LastError;
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    void OnDestroy()
    {
        Disconnect();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
