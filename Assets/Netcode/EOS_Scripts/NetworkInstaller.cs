
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.NetworkServices;
using Netcode.EOS_Scripts.Services;
using UnityEngine;
using Zenject;

public class NetworkInstaller : MonoInstaller
{
    [SerializeField] private EOSMirrorNetworkManager _networkManager;
    [SerializeField] private EOSLobby _eosLobby;

    public override void InstallBindings()
    {
        Container.Bind<EOSMirrorNetworkManager>().FromInstance(_networkManager).AsSingle();
        Container.Bind<EOSLobby>().FromInstance(_eosLobby).AsSingle();

        Container.Bind<INetworkService>().To<EOSMirrorNetworkService>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<EOSMirrorLobbyService>().AsSingle().NonLazy();

        Container.Bind<INetworkPlayerCameraController>().To<EOSMirrorPlayerCameraController>().AsSingle().NonLazy();
        Container.Bind<INetworkSoundService>().To<EOSMirrorSoundService>().AsSingle().NonLazy();
        Container.Bind<INetworkInventoryService>().To<EOSMirrorInventoryService>().AsSingle().NonLazy();

        //qualquer coisa apagar z
        Container.Bind<INetworkQuickTimeEventService>().To<EOSMirrorQuickTimeEventService>().AsSingle().NonLazy();

        Container.QueueForInject(_networkManager);
    }
}
