using CoreDomain.Scripts.Services.Logger.Base;
using Mirror;
using UnityEngine;
using Zenject;

public class EOSMirrorNetworkManager : NetworkManager
{
    private DiContainer _container;

    [Inject]
    private void Setup(DiContainer container)
    {
        _container = container;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        LogService.Log("Mirror server started.");
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        LogService.Log("Mirror host started.");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
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

        if (_container == null)
        {
            LogService.LogError("Zenject container was not injected into EOSMirrorNetworkManager.");
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

        GameObject player = _container.InstantiatePrefab(playerPrefab, spawnPosition, spawnRotation, null);
        player.name = "NetworkPlayer_" + connection.connectionId;

        NetworkServer.AddPlayerForConnection(connection, player);

        LogService.Log("Player spawned. Connection ID: " + connection.connectionId + " | Position: " + spawnPosition);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient connection)
    {
        LogService.Log("Player disconnected. Connection ID: " + connection.connectionId);
        base.OnServerDisconnect(connection);
    }
}