using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using Zenject;

public class GameplayInstaller : MonoInstaller {
    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<IGamePlayInitiator>().To<GamePlayInitiator>().AsSingle().NonLazy();
    }

    private void BindControllers() {

    }
}
