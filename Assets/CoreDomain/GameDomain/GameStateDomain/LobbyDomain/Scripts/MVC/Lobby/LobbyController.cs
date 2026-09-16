using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.SceneService;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;

public class LobbyController : ILobbyController
{
    private LobbyView _lobbyView;
    private readonly IStateMachineService _stateMachineService;
    private readonly GamePlayState.Factory _gamePlayStateFactory;

    public LobbyController(LobbyView lobbyView, IStateMachineService stateMachineService, GamePlayState.Factory gamePlayStateFactory) {
        _stateMachineService = stateMachineService;
        _gamePlayStateFactory = gamePlayStateFactory; _lobbyView = GameObject.Instantiate(lobbyView);
        _lobbyView.SetUp(LoadZooScene);
    }

    private void LoadZooScene(ScenesType sceneToLoad) {
        LogService.LogWarning("Tentando dar load na cena de zoo");
        _stateMachineService.SwitchState(_gamePlayStateFactory.Create(new GamePlayInitatorEnterData((int)sceneToLoad)));
    }
}
