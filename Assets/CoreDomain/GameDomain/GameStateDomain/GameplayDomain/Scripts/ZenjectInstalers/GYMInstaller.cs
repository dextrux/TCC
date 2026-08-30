using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Services;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.ZenjectInstalers
{
    public class GYMInstaller : MonoInstaller {
        [Header("Noise Settings")]
        [SerializeField] private NoiseDecaySettings noiseDecaySettings;
        public override void InstallBindings() {
            BindServices();
            BindControllers();
        }

        private void BindServices() {
            
        }

        private void BindControllers() {
            Container.Bind<NoiseDecaySettings>()
                .FromInstance(noiseDecaySettings)
                .AsSingle();
            
            Container.Bind<INoiseManager>()
                .To<NoiseManager>()
                .AsSingle();
        }
    }
}
