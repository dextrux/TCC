using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Services;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller {

    [SerializeField] private NoiseDecaySettings noiseDecaySettings;
    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<IGamePlayInitiator>().To<GamePlayInitiator>().AsSingle().NonLazy();
        Container.Bind<NoiseDecaySettings>().FromInstance(noiseDecaySettings).AsSingle();
        Container.Bind<INoiseManager>().To<NoiseManager>().AsSingle();
    }

    private void BindControllers() {
        Container.BindInterfacesTo<LevelScenarioController>().AsSingle().NonLazy();
    }
}
