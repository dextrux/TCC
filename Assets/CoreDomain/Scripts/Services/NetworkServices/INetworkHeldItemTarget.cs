namespace CoreDomain.Scripts.Services.NetworkServices
{
    public interface INetworkHeldItemTarget
    {
        void ServerSetHeldItem(int itemId);
        void ServerSetHeldItemActive(bool active);
    }
}
