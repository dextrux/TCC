using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Services;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.ZenjectInstalers
{
    public class GYMInstaller : MonoInstaller
    {
        [SerializeField] private AudioSetting audioSetting;
        
        public override void InstallBindings() {
            BindServices();
            BindControllers();
        }

        private void BindServices() {
        
        }

        private void BindControllers()
        {
            BindAudio();
        }

        private void BindAudio()
        {
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
    }
}
