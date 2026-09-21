using Unity.Netcode;

public interface IResourceSpawner {
    void SpawnOnServerRPC(int resourceID, ulong clientID);

    event System.Action<NetworkObject> OnResourceSpawned;
}