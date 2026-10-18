using Mirror;

namespace Netcode.EOS_Scripts.Messages
{
    public struct InventorySelectSlotMessage : NetworkMessage
    {
        public int Index;
    }

    public struct InventoryScrollMessage : NetworkMessage
    {
        public int Direction;
    }
}
