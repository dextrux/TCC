using CoreDomain.GameDomain.GameStateDomain.LobbyDomain.Scripts.Initiator;
using Unity;
using UnityEngine;
using Zenject;

public class LobbyInstaller : MonoInstaller {
    [SerializeField] private LobbyView _lobbyView;
    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<ILobbyInitiator>().To<LobbyInitiator>().AsSingle().NonLazy();
    }

    private void BindControllers() {
        Container.Bind<ILobbyController>().To<LobbyController>().AsSingle().WithArguments(_lobbyView).NonLazy();
    }
}
