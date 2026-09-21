using System;
using Unity.Netcode;
using UnityEngine;

public class ResourceSpawner :
    NetworkBehaviour,
    IResourceSpawner {
    [SerializeField]
    private NetworkObject[] resourcePrefabs;

    public event Action<NetworkObject>
        OnResourceSpawned;

    [Rpc(SendTo.Server)]
    public void SpawnOnServerRPC(
        int resourceID,
        ulong clientID) {
        if (resourceID < 0 ||
            resourceID >= resourcePrefabs.Length) {
            Debug.LogError(
                $"Resource ID inválido: {resourceID}"
            );

            return;
        }

        NetworkObject objectToSpawn =
            Instantiate(
                resourcePrefabs[resourceID]
            );

        objectToSpawn.SpawnWithOwnership(
            clientID,
            true
        );

        NotifyClientResourceSpawnedRpc(
            objectToSpawn.NetworkObjectId,
            RpcTarget.Single(
                clientID,
                RpcTargetUse.Temp
            )
        );
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void NotifyClientResourceSpawnedRpc(
        ulong networkObjectId,
        RpcParams rpcParams = default) {
        if (!NetworkManager.Singleton
            .SpawnManager
            .SpawnedObjects
            .TryGetValue(
                networkObjectId,
                out NetworkObject spawnedObject)) {
            Debug.LogError(
                $"NetworkObject {networkObjectId} " +
                $"não encontrado no cliente."
            );

            return;
        }

        OnResourceSpawned?.Invoke(
            spawnedObject
        );
    }
}