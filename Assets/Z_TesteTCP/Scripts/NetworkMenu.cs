using UnityEngine;

[RequireComponent(typeof(TcpServer))]
[RequireComponent(typeof(TcpClient))]
[RequireComponent(typeof(NatTraversal))]
[RequireComponent(typeof(PublicIpResolver))]
public class NetworkMenu : MonoBehaviour
{
    private TcpServer server;
    private TcpClient client;
    private NatTraversal natTraversal;
    private PublicIpResolver publicIpResolver;

    private string hostPortText = "7777";
    private string serverIp = "127.0.0.1";
    private string serverPortText = "7777";

    private string localIp = "";
    private string localMessage = "";

    void Awake()
    {
        server = GetComponent<TcpServer>();
        client = GetComponent<TcpClient>();
        natTraversal = GetComponent<NatTraversal>();
        publicIpResolver = GetComponent<PublicIpResolver>();
    }

    void Start()
    {
        localIp = NetworkAddressUtility.GetLocalIPv4Address();
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 18;
        GUI.skin.textField.fontSize = 18;

        GUILayout.BeginArea(new Rect(20, 20, 520, 760), GUI.skin.box);

        GUILayout.Label("FPS MULTIPLAYER TCP");
        GUILayout.Space(10);

        if (client.Connected)
        {
            DrawConnectedMenu();
        }
        else
        {
            DrawConnectionMenu();
        }

        GUILayout.EndArea();
    }

    private void DrawConnectionMenu()
    {
        DrawHostMenu();

        GUILayout.Space(20);

        DrawClientMenu();

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(localMessage))
        {
            GUILayout.Label(localMessage);
        }

        if (!string.IsNullOrEmpty(client.Message))
        {
            GUILayout.Label(client.Message);
        }
    }

    private void DrawHostMenu()
    {
        GUILayout.Label("CRIAR SERVIDOR / HOST");

        GUILayout.Space(5);

        GUILayout.Label("IP local desta máquina:");
        GUILayout.Label(localIp);

        GUILayout.Label("Porta do host:");
        hostPortText = GUILayout.TextField(hostPortText);

        GUILayout.Space(8);

        if (!server.IsRunning)
        {
            if (GUILayout.Button("INICIAR COMO SERVIDOR", GUILayout.Height(42)))
            {
                StartHost();
            }

            return;
        }

        GUILayout.Label(server.Status);

        DrawHostAddresses();

        if (GUILayout.Button("CONECTAR JOGADOR LOCAL", GUILayout.Height(40)))
        {
            ConnectLocalPlayer();
        }

        if (GUILayout.Button("PARAR SERVIDOR", GUILayout.Height(40)))
        {
            StopHost();
        }
    }

    private void DrawClientMenu()
    {
        GUILayout.Label("CONECTAR COMO CLIENTE");

        GUILayout.Space(5);

        GUILayout.Label("IP do servidor:");
        serverIp = GUILayout.TextField(serverIp);

        GUILayout.Label("Porta do servidor:");
        serverPortText = GUILayout.TextField(serverPortText);

        GUILayout.Space(8);

        if (GUILayout.Button("CONECTAR COMO CLIENTE", GUILayout.Height(42)))
        {
            ConnectAsClient();
        }
    }

    private void DrawConnectedMenu()
    {
        GUILayout.Label("Conectado");
        GUILayout.Label("ID do jogador: " + client.LocalPlayerId);
        GUILayout.Label(client.Message);

        if (server.IsRunning)
        {
            GUILayout.Space(15);

            GUILayout.Label("Esta instância é o HOST.");
            GUILayout.Label(server.Status);

            GUILayout.Space(10);

            DrawHostAddresses();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("DESCONECTAR", GUILayout.Height(40)))
        {
            client.Disconnect();
            ShowCursor();
        }

        if (server.IsRunning && GUILayout.Button("PARAR SERVIDOR", GUILayout.Height(40)))
        {
            StopHost();
        }
    }

    private void DrawHostAddresses()
    {
        int port;

        if (!TryGetPort(hostPortText, out port))
        {
            return;
        }

        GUILayout.Space(10);

        GUILayout.Label("CONEXÃO NA MESMA REDE");
        GUILayout.Label("IP local:");
        GUILayout.Label(localIp);

        GUILayout.Label("Endereço local:");
        GUILayout.Label(localIp + ":" + port);

        if (GUILayout.Button("COPIAR ENDEREÇO LOCAL", GUILayout.Height(35)))
        {
            GUIUtility.systemCopyBuffer = localIp + ":" + port;
            localMessage = "Endereço local copiado.";
        }

        GUILayout.Space(15);

        GUILayout.Label("ACESSO PELA INTERNET");

        DrawPublicIp(port);
        DrawNatStatus(port);
    }

    private void DrawPublicIp(int port)
    {
        if (publicIpResolver.IsBusy)
        {
            GUILayout.Label("Buscando IP público...");
            return;
        }

        if (string.IsNullOrEmpty(publicIpResolver.PublicIp))
        {
            GUILayout.Label(publicIpResolver.StatusMessage);
            return;
        }

        GUILayout.Label("IP público:");
        GUILayout.Label(publicIpResolver.PublicIp);

        if (!natTraversal.PortMapped)
        {
            return;
        }

        string publicAddress = publicIpResolver.PublicIp + ":" + port;

        GUILayout.Label("Endereço público:");
        GUILayout.Label(publicAddress);

        if (GUILayout.Button("COPIAR ENDEREÇO PÚBLICO", GUILayout.Height(35)))
        {
            GUIUtility.systemCopyBuffer = publicAddress;
            localMessage = "Endereço público copiado.";
        }
    }

    private void DrawNatStatus(int port)
    {
        GUILayout.Space(5);

        GUILayout.Label("Abertura automática da porta:");
        GUILayout.Label(natTraversal.StatusMessage);

        if (!string.IsNullOrEmpty(natTraversal.RouterExternalIp))
        {
            GUILayout.Label("IP WAN informado pelo roteador:");
            GUILayout.Label(natTraversal.RouterExternalIp);
        }

        if (!natTraversal.IsBusy && !natTraversal.PortMapped)
        {
            if (GUILayout.Button("TENTAR ABRIR PORTA NOVAMENTE", GUILayout.Height(35)))
            {
                natTraversal.OpenPort(port);
            }
        }

        if (HasPossibleCarrierGradeNat())
        {
            GUILayout.Space(10);
            GUILayout.Label("Possível CGNAT ou duplo NAT detectado.");
        }
    }

    private void StartHost()
    {
        int port;

        if (!TryGetPort(hostPortText, out port))
        {
            localMessage = "Porta do host inválida.";
            return;
        }

        localIp = NetworkAddressUtility.GetLocalIPv4Address();

        if (!server.StartServer("0.0.0.0", port))
        {
            localMessage = server.Status;
            return;
        }

        localMessage = "Servidor iniciado.";

        natTraversal.OpenPort(port);
        publicIpResolver.Refresh();

        client.Connect("127.0.0.1", port);
    }

    private void StopHost()
    {
        natTraversal.ClosePortMapping();
        client.Disconnect();
        server.StopServer();
        publicIpResolver.Clear();

        localMessage = "";
        ShowCursor();
    }

    private void ConnectLocalPlayer()
    {
        int port;

        if (TryGetPort(hostPortText, out port))
        {
            client.Connect("127.0.0.1", port);
        }
    }

    private void ConnectAsClient()
    {
        int port;

        if (string.IsNullOrWhiteSpace(serverIp))
        {
            localMessage = "Digite o IP do servidor.";
            return;
        }

        if (!TryGetPort(serverPortText, out port))
        {
            localMessage = "Porta do servidor inválida.";
            return;
        }

        localMessage = "Conectando em " + serverIp.Trim() + ":" + port + "...";

        client.Connect(serverIp.Trim(), port);
    }

    private bool HasPossibleCarrierGradeNat()
    {
        if (string.IsNullOrEmpty(publicIpResolver.PublicIp) || string.IsNullOrEmpty(natTraversal.RouterExternalIp))
        {
            return false;
        }

        if (NetworkAddressUtility.IsNonPublicAddress(natTraversal.RouterExternalIp))
        {
            return true;
        }

        return publicIpResolver.PublicIp.Trim() != natTraversal.RouterExternalIp.Trim();
    }

    private bool TryGetPort(string text, out int port)
    {
        if (!int.TryParse(text, out port))
        {
            return false;
        }

        return port >= 1 && port <= 65535;
    }

    private void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
