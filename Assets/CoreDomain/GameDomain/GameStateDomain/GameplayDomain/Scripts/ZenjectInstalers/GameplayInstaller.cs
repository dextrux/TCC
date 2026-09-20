using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Services;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Services;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller {
    [SerializeField] private AudioSetting audioSetting;
    [SerializeField] private NoiseDecaySettings noiseDecaySettings;
    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<IGamePlayInitiator>().To<GamePlayInitiator>().AsSingle().NonLazy();
        
        /*Container.Bind<AudioSetting>().FromInstance(audioSetting).AsSingle();

        Container.BindFactory<AudioSourcePooled, AudioSourcePooled.Factory>()
            .FromComponentInNewPrefab(audioSetting.sourcePrefab)
            .UnderTransformGroup("AudioSources");

        Container.BindInstance(audioSetting.defaultGroup).WhenInjectedInto<AudioSourcePool>();
        Container.BindInstance(audioSetting.mixer).WhenInjectedInto<AudioManager>();
        Container.BindInstance(audioSetting.registry).WhenInjectedInto<AudioManager>();

        Container.Bind<IAudioSourcePool>().To<AudioSourcePool>().AsSingle();
        Container.Bind<IAudioManager>().To<AudioManager>().AsSingle().NonLazy();

        Container.Bind<NetworkAudioRelay>().FromInstance(audioSetting.networkAudioRelay).AsSingle();

        Container.Bind<IInitializable>()
            .To<AudioPoolWarmup>()
            .AsSingle()
            .WithArguments(audioSetting.prewarmCount);
        Container.Bind<NoiseDecaySettings>().FromInstance(noiseDecaySettings).AsSingle();

        Container.Bind<INoiseManager>().To<NoiseManager>().AsSingle();*/
    }

    private void BindControllers() {
        Container.BindInterfacesTo<LevelScenarioController>().AsSingle().NonLazy();
        Debug.LogWarning("BindControllers Gameplay");
    }
}
