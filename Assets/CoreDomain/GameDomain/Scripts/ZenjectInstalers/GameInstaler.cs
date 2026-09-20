using CoreDomain.GameDomain.Scripts.AudioSystem;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.AudioSystem.Services;
using CoreDomain.GameDomain.Scripts.GameInitiator;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.GameDomain.Scripts.States.LobbyState;
using UnityEngine;
using Zenject;

public class GameInstaler : MonoInstaller {
    [SerializeField] private AudioSetting audioSetting;

    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<IGameInitiator>().To<GameInitiator>().AsSingle().NonLazy();
        Container.BindFactory<GamePlayInitatorEnterData, GamePlayState, GamePlayState.Factory>();
        Container.BindFactory<LobbyInitiatorEnterData, LobbyState, LobbyState.Factory>().AsSingle().NonLazy();

        Container.Bind<AudioSetting>().FromInstance(audioSetting).AsSingle();

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
    }

    private void BindControllers() {

    }
}
