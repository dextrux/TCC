namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.GameplayNetwork
{
    /// <summary>
    /// Liga os servicos de rede (som, inventario, save) aos sistemas da gameplay.
    /// O GamePlayInitiator so decide QUANDO chamar; o COMO fica aqui.
    /// </summary>
    public interface IGameplayNetworkBridge
    {
        void Configure(bool offline);
        void Release();
    }
}
