
using System;

namespace CoreDomain.Scripts.Services.NetworkService
{
    public interface INetworkLobbyService
    {
        bool IsInitialized { get; }
        bool IsBusy { get; }
        bool IsConnected { get; }
        bool IsHost { get; }
        bool IsLocalSession { get; }

        string LobbyCode { get; }
        string HostProductUserId { get; }
        string Status { get; }

        int ConnectedPlayers { get; }
        int MaxPlayers { get; }

        event Action LobbyChanged;
        event Action GameStarted;

        void CreateLobby();
        void JoinLobby(string code);

        void CreateLocalHost();
        void JoinLocalHost(string address);

        void StartGame();
        void LeaveLobby();
    }
}
