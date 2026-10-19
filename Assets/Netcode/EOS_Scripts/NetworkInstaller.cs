using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.NetworkServices;
using Netcode.EOS_Scripts.Services;
using UnityEngine;
using Zenject;

public class NetworkInstaller : MonoInstaller
{
    [SerializeField] private EOSMirrorNetworkManager _networkManager;

    public override void InstallBindings()
    {
        Container.Bind<EOSMirrorNetworkManager>().FromInstance(_networkManager).AsSingle();
        Container.Bind<INetworkService>().To<EOSMirrorNetworkService>().AsSingle().NonLazy();
        Container.Bind<INetworkPlayerCameraController>().To<EOSMirrorPlayerCameraController>().AsSingle().NonLazy();
        Container.Bind<INetworkSoundService>().To<EOSMirrorSoundService>().AsSingle().NonLazy();
        Container.Bind<INetworkInventoryService>().To<EOSMirrorInventoryService>().AsSingle().NonLazy();

        Container.QueueForInject(_networkManager);
    }
}
