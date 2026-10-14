using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;

public class LobbyController : ILobbyController
{
    private readonly LobbyView _lobbyView;
    private readonly IStateMachineService _stateMachineService;
    private readonly GamePlayState.Factory _gamePlayStateFactory;

    public LobbyController(LobbyView lobbyView, IStateMachineService stateMachineService, GamePlayState.Factory gamePlayStateFactory)
    {
        _stateMachineService = stateMachineService;
        _gamePlayStateFactory = gamePlayStateFactory;
        _lobbyView = GameObject.Instantiate(lobbyView);

        _lobbyView.SetUp(LoadZooScene, LoadGYMHost, LoadGYMClient);
    }

    private void LoadZooScene()
    {
        SwitchToGameplay("ZooScene", NetworkSessionMode.Offline, string.Empty);
    }

    private void LoadGYMHost()
    {
        SwitchToGameplay("GYMScene", NetworkSessionMode.Host, string.Empty);
    }

    private void LoadGYMClient(string hostProductUserId)
    {
        if (string.IsNullOrWhiteSpace(hostProductUserId))
        {
            LogService.LogError("Host Product User ID is empty.");
            return;
        }

        SwitchToGameplay("GYMScene", NetworkSessionMode.Client, hostProductUserId);
    }

    private void SwitchToGameplay(string sceneToLoad, NetworkSessionMode sessionMode, string hostProductUserId)
    {
        GamePlayInitatorEnterData enterData = new GamePlayInitatorEnterData(sceneToLoad, sessionMode, hostProductUserId);
        _stateMachineService.SwitchState(_gamePlayStateFactory.Create(enterData));
    }
}