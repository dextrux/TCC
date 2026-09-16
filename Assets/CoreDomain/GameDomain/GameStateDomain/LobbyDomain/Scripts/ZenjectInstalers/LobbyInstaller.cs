using CoreDomain.GameDomain.GameStateDomain.LobbyDomain.Scripts.Initiator;
using Zenject;

public class LobbyInstaller : MonoInstaller {
    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<ILobbyInitiator>().To<LobbyInitiator>().AsSingle().NonLazy();
    }

    private void BindControllers() {

    }
}
