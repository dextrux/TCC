using EpicTransport;
using Mirror;
using UnityEngine;

public class EOSMirrorConnectionMenu : MonoBehaviour
{
    private string localProductUserId = "";
    private string hostProductUserId = "";
    private string statusMessage = "Inicializando Epic Online Services...";

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = NetworkManager.singleton;
    }

    private void Update()
    {
        RefreshEOSState();
    }

    private void RefreshEOSState()
    {
        if (!EOSSDKComponent.Initialized)
        {
            return;
        }

        if (!string.IsNullOrEmpty(localProductUserId))
        {
            return;
        }

        if (EOSSDKComponent.LocalUserProductId == null)
        {
            return;
        }

        localProductUserId = EOSSDKComponent.LocalUserProductId.ToString();
        statusMessage = "Epic Online Services conectado.";

        Debug.Log("EOS Product User ID: " + localProductUserId);
    }

    private void OnGUI()
    {
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 18;
        GUI.skin.textField.fontSize = 18;

        GUILayout.BeginArea(new Rect(20f, 20f, 560f, 520f), GUI.skin.box);

        GUILayout.Label("EOS + MIRROR");

        GUILayout.Space(10f);
        GUILayout.Label(statusMessage);

        GUILayout.Space(15f);
        GUILayout.Label("MEU PRODUCT USER ID:");

        if (string.IsNullOrEmpty(localProductUserId))
        {
            GUILayout.Label("Aguardando EOS...");
        }
        else
        {
            GUILayout.TextArea(localProductUserId);

            if (GUILayout.Button("COPIAR MEU ID", GUILayout.Height(35f)))
            {
                GUIUtility.systemCopyBuffer = localProductUserId;
            }
        }

        GUILayout.Space(20f);

        if (!NetworkClient.active && !NetworkServer.active)
        {
            DrawConnectionOptions();
        }
        else
        {
            DrawConnectedState();
        }

        GUILayout.EndArea();
    }

    private void DrawConnectionOptions()
    {
        if (GUILayout.Button("INICIAR HOST", GUILayout.Height(45f)))
        {
            StartHost();
        }

        GUILayout.Space(20f);

        GUILayout.Label("PRODUCT USER ID DO HOST:");
        hostProductUserId = GUILayout.TextField(hostProductUserId);

        if (GUILayout.Button("CONECTAR COMO CLIENTE", GUILayout.Height(45f)))
        {
            StartClient();
        }
    }

    private void DrawConnectedState()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            GUILayout.Label("Estado: HOST");
        }
        else if (NetworkClient.active)
        {
            GUILayout.Label("Estado: CLIENTE");
        }
        else if (NetworkServer.active)
        {
            GUILayout.Label("Estado: SERVIDOR");
        }

        GUILayout.Space(10f);

        if (GUILayout.Button("DESCONECTAR", GUILayout.Height(40f)))
        {
            Disconnect();
        }
    }

    public void StartHost()
    {
        if (!CanStartNetwork())
        {
            return;
        }

        statusMessage = "Iniciando host...";
        networkManager.StartHost();
    }

    public void StartClient()
    {
        if (!CanStartNetwork())
        {
            return;
        }

        string targetProductUserId = hostProductUserId.Trim();

        if (string.IsNullOrEmpty(targetProductUserId))
        {
            statusMessage = "Digite o Product User ID do host.";
            return;
        }

        networkManager.networkAddress = targetProductUserId;

        statusMessage = "Conectando ao host...";
        networkManager.StartClient();
    }

    public void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            networkManager.StopHost();
        }
        else if (NetworkClient.active)
        {
            networkManager.StopClient();
        }
        else if (NetworkServer.active)
        {
            networkManager.StopServer();
        }

        statusMessage = "Desconectado.";
    }

    private bool CanStartNetwork()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.singleton;
        }

        if (networkManager == null)
        {
            statusMessage = "NetworkManager não encontrado.";
            return false;
        }

        if (!EOSSDKComponent.Initialized)
        {
            statusMessage = "Epic Online Services ainda está inicializando.";
            return false;
        }

        if (string.IsNullOrEmpty(localProductUserId))
        {
            RefreshEOSState();
        }

        if (string.IsNullOrEmpty(localProductUserId))
        {
            statusMessage = "Product User ID ainda não está disponível.";
            return false;
        }

        return true;
    }
}