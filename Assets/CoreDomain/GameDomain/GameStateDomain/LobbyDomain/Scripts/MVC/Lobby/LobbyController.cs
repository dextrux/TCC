
using CoreDomain.GameDomain.Scripts.Inputs;
using CoreDomain.GameDomain.Scripts.States.GamePlayState;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;

public class LobbyController : ILobbyController
{
    private readonly LobbyView _lobbyView;
    private readonly IStateMachineService _stateMachineService;
    private readonly GamePlayState.Factory _gamePlayStateFactory;
    private readonly INetworkLobbyService _networkLobbyService;
    private readonly IInputManager _inputManager;

    private bool _transitioning;

    public LobbyController(LobbyView lobbyView, Transform viewParent, IStateMachineService stateMachineService, GamePlayState.Factory gamePlayStateFactory, INetworkLobbyService networkLobbyService, IInputManager inputManager)
    {
        _stateMachineService = stateMachineService;
        _gamePlayStateFactory = gamePlayStateFactory;
        _networkLobbyService = networkLobbyService;
        _inputManager = inputManager;

        _lobbyView = GameObject.Instantiate(lobbyView, viewParent);

        _lobbyView.SetUp(LoadZooScene, CreateLobby, JoinLobby, StartGame, LeaveLobby, CreateLocalHost, JoinLocalHost);

        _networkLobbyService.LobbyChanged += RefreshLobbyView;
        _networkLobbyService.GameStarted += HandleGameStarted;

        RefreshLobbyView();
    }

    private void LoadZooScene()
    {
        if (_transitioning || _networkLobbyService.IsBusy || _networkLobbyService.IsConnected)
        {
            return;
        }

        SwitchToGameplay("ZooScene", NetworkSessionMode.Offline, string.Empty);
    }

    private void CreateLobby()
    {
        if (_transitioning)
        {
            return;
        }

        _networkLobbyService.CreateLobby();
    }

    private void JoinLobby(string code)
    {
        if (_transitioning)
        {
            return;
        }

        _networkLobbyService.JoinLobby(code);
    }

    private void CreateLocalHost()
    {
        if (_transitioning)
        {
            return;
        }

        _networkLobbyService.CreateLocalHost();
    }

    private void JoinLocalHost(string address)
    {
        if (_transitioning)
        {
            return;
        }

        _networkLobbyService.JoinLocalHost(address);
    }

    private void StartGame()
    {
        if (_transitioning)
        {
            return;
        }

        _networkLobbyService.StartGame();
    }

    private void LeaveLobby()
    {
        if (_transitioning)
        {
            return;
        }

        _networkLobbyService.LeaveLobby();
    }

    private void HandleGameStarted()
    {
        if (_transitioning)
        {
            return;
        }

        NetworkSessionMode mode = _networkLobbyService.IsHost ? NetworkSessionMode.Host : NetworkSessionMode.Client;

        SwitchToGameplay("Level2Scene", mode, _networkLobbyService.HostProductUserId);
    }

    private void RefreshLobbyView()
    {
        if (_lobbyView == null)
        {
            return;
        }

        _lobbyView.Refresh(_networkLobbyService.IsBusy, _networkLobbyService.IsConnected, _networkLobbyService.IsHost, _networkLobbyService.ConnectedPlayers, _networkLobbyService.MaxPlayers, _networkLobbyService.LobbyCode, _networkLobbyService.Status, _networkLobbyService.IsLocalSession);
    }

    private void SwitchToGameplay(string sceneToLoad, NetworkSessionMode sessionMode, string hostProductUserId)
    {
        if (_transitioning)
        {
            return;
        }

        _transitioning = true;

        _inputManager.DisableUIInput();
        _lobbyView.gameObject.SetActive(false);

        GamePlayInitatorEnterData enterData = new GamePlayInitatorEnterData(sceneToLoad, sessionMode, hostProductUserId);

        _stateMachineService.SwitchState(_gamePlayStateFactory.Create(enterData));
    }

    public void Dispose()
    {
        _networkLobbyService.LobbyChanged -= RefreshLobbyView;
        _networkLobbyService.GameStarted -= HandleGameStarted;

        if (_lobbyView != null)
        {
            GameObject.Destroy(_lobbyView.gameObject);
        }
    }
}
