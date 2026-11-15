using Mirror;

namespace Netcode.EOS_Scripts.Messages
{
    public struct QTEStartMessage : NetworkMessage
    {
    }

    public struct QTEInputMessage : NetworkMessage
    {
        public int Key;
    }

    public struct QTEUpdateMessage : NetworkMessage
    {
        public string Letter;
        public int Step;
        public int Result;
    }
}