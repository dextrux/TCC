using CoreDomain.GameDomain.Scripts.GameInitiator;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.GameDomain.Scripts.States.LobbyState;
using Zenject;

public class GameInstaler : MonoInstaller {
    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<IGameInitiator>().To<GameInitiator>().AsSingle().NonLazy();
        Container.BindFactory<GamePlayInitatorEnterData, GamePlayState, GamePlayState.Factory>();
        Container.BindFactory<LobbyInitiatorEnterData, LobbyState, LobbyState.Factory>().AsSingle().NonLazy();
    }

    private void BindControllers() {

    }
}
