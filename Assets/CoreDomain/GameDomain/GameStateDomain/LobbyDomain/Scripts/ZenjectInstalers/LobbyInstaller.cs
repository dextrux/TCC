
using UnityEngine;
using Zenject;

public class LobbyInstaller : MonoInstaller
{
    [SerializeField] private LobbyView _lobbyView;

    public override void InstallBindings()
    {
        BindControllers();
    }

    private void BindControllers()
    {
        Container.Bind<ILobbyController>().To<LobbyController>().AsSingle().WithArguments(_lobbyView, transform).NonLazy();
    }
}
