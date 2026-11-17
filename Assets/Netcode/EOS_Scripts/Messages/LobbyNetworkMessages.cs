
using Mirror;

public struct LobbyPlayerCountMessage : NetworkMessage
{
    public int PlayerCount;
}

public struct LobbyStartGameMessage : NetworkMessage
{
}
