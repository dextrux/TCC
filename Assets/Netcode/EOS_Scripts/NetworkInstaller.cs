using CoreDomain.Scripts.Services.NetworkService;
using UnityEngine;
using Zenject;

public class NetworkInstaller : MonoInstaller
{
    [SerializeField] private EOSMirrorNetworkManager _networkManager;

    public override void InstallBindings()
    {
        Container.Bind<EOSMirrorNetworkManager>().FromInstance(_networkManager).AsSingle();
        Container.Bind<INetworkService>().To<EOSMirrorNetworkService>().AsSingle().NonLazy();
    }
}