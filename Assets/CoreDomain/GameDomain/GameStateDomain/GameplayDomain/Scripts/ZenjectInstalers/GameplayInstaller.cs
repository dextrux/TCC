using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Initiator;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.ScriptableObject;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Services;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Services;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Services;
using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken;
using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller
{
    [SerializeField] private NoiseDecaySettings noiseDecaySettings;
    [SerializeField] private InventoryConfigurationSo _inventoryConfiguration;

    public override void InstallBindings()
    {
        BindServices();
        BindControllers();
    }

    private void BindServices()
    {
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<IGamePlayInitiator>().To<GamePlayInitiator>().AsSingle().NonLazy();
        Container.Bind<NoiseDecaySettings>().FromInstance(noiseDecaySettings).AsSingle();
        Container.Bind<INoiseManager>().To<NoiseManager>().AsSingle();
        
        Container.BindInstance(_inventoryConfiguration).AsSingle();
        Container.Bind<IInventoryManager>().To<InventoryManager>().AsSingle();
        Container.Bind<ObjectFactory>().AsSingle();
    }

    private void BindControllers()
    {
        Container.BindInterfacesTo<LevelScenarioController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<WorldObjectSpawner>().AsSingle();
    }
}