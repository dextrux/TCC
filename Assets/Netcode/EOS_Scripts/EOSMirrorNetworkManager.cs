using Mirror;
using UnityEngine;

public class EOSMirrorNetworkManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Mirror server started.");
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("Mirror host started.");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Mirror client connected.");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient connection)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned.");
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

        Debug.Log("Player spawned. Connection ID: " + connection.connectionId + " | Position: " + spawnPosition);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient connection)
    {
        Debug.Log("Player disconnected. Connection ID: " + connection.connectionId);
        base.OnServerDisconnect(connection);
    }
}