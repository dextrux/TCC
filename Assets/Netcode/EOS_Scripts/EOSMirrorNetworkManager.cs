using CoreDomain.Scripts.Services.Logger.Base;
using Mirror;
using UnityEngine;

public class EOSMirrorNetworkManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<PlayerCameraInputMessage>(OnPlayerCameraInputMessage);

        LogService.Log("Mirror server started.");
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<PlayerCameraInputMessage>();

        base.OnStopServer();
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

        NetworkServer.AddPlayerForConnection(connection, player);

        LogService.Log("Player spawned. Connection ID: " + connection.connectionId + " | Position: " + spawnPosition);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient connection)
    {
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
}