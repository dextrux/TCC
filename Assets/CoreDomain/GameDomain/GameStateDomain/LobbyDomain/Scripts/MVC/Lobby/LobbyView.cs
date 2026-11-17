
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _lobbyPanel;

    [Header("Existing Buttons")]
    [SerializeField] private Button _zooButton;
    [SerializeField] private Button _GYMClientButton;
    [SerializeField] private Button _GYMServerButton;

    [Header("Lobby Input")]
    [SerializeField] private TMP_InputField _hostProductUserIdInputField;

    [Header("Lobby Buttons")]
    [SerializeField] private Button _startGameButton;
    [SerializeField] private Button _leaveLobbyButton;
    [SerializeField] private Button _copyLobbyCodeButton;

    [Header("Lobby Information")]
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private TMP_Text _lobbyCodeText;
    [SerializeField] private TMP_Text _playersText;

    private Action _onZooClick;
    private Action _onGYMHostClick;
    private Action<string> _onGYMClientClick;
    private Action _onStartGameClick;
    private Action _onLeaveLobbyClick;

    private string _currentLobbyCode = "";

    private bool _showLobbyPanel;
    private bool _wasConnected;

    private void Awake()
    {
        SetLobbyPanelVisible(false);
    }

    public void SetUp(Action onZooClick, Action onGYMHostClick, Action<string> onGYMClientClick, Action onStartGameClick, Action onLeaveLobbyClick)
    {
        _onZooClick = onZooClick;
        _onGYMHostClick = onGYMHostClick;
        _onGYMClientClick = onGYMClientClick;
        _onStartGameClick = onStartGameClick;
        _onLeaveLobbyClick = onLeaveLobbyClick;

        _zooButton.onClick.AddListener(OnZooClick);
        _GYMServerButton.onClick.AddListener(OnGYMServerClick);
        _GYMClientButton.onClick.AddListener(OnGYMClientClick);

        if (_startGameButton != null)
        {
            _startGameButton.onClick.AddListener(OnStartGameClick);
        }

        if (_leaveLobbyButton != null)
        {
            _leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClick);
        }

        if (_copyLobbyCodeButton != null)
        {
            _copyLobbyCodeButton.onClick.AddListener(OnCopyLobbyCodeClick);
        }
    }

    public void Refresh(bool isBusy, bool isConnected, bool isHost, int connectedPlayers, int maxPlayers, string lobbyCode, string status)
    {
        _currentLobbyCode = lobbyCode;

        if (_showLobbyPanel && _wasConnected && !isConnected && !isBusy)
        {
            SetLobbyPanelVisible(false);
        }

        if (isConnected)
        {
            _wasConnected = true;
        }

        bool canSelectMode = !isBusy && !isConnected;

        _zooButton.interactable = canSelectMode;
        _GYMServerButton.interactable = canSelectMode;
        _GYMClientButton.interactable = canSelectMode;

        if (_hostProductUserIdInputField != null)
        {
            _hostProductUserIdInputField.interactable = canSelectMode;
        }

        if (_startGameButton != null)
        {
            _startGameButton.gameObject.SetActive(isConnected && isHost);
            _startGameButton.interactable = !isBusy && isConnected && isHost && connectedPlayers == maxPlayers;
        }

        if (_leaveLobbyButton != null)
        {
            _leaveLobbyButton.gameObject.SetActive(_showLobbyPanel);
            _leaveLobbyButton.interactable = !isBusy;
        }

        if (_copyLobbyCodeButton != null)
        {
            _copyLobbyCodeButton.gameObject.SetActive(isConnected && isHost);
        }

        if (_statusText != null)
        {
            _statusText.text = status;
        }

        if (_lobbyCodeText != null)
        {
            _lobbyCodeText.text = string.IsNullOrEmpty(lobbyCode) ? "Código: ------" : "Código: " + lobbyCode;
        }

        if (_playersText != null)
        {
            _playersText.text = isConnected ? "Jogadores: " + connectedPlayers + "/" + maxPlayers : "Jogadores: 0/" + maxPlayers;
        }
    }

    private void SetLobbyPanelVisible(bool visible)
    {
        _showLobbyPanel = visible;

        if (_mainMenuPanel != null)
        {
            _mainMenuPanel.SetActive(!visible);
        }

        if (_lobbyPanel != null)
        {
            _lobbyPanel.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        if (_zooButton != null)
        {
            _zooButton.onClick.RemoveListener(OnZooClick);
        }

        if (_GYMServerButton != null)
        {
            _GYMServerButton.onClick.RemoveListener(OnGYMServerClick);
        }

        if (_GYMClientButton != null)
        {
            _GYMClientButton.onClick.RemoveListener(OnGYMClientClick);
        }

        if (_startGameButton != null)
        {
            _startGameButton.onClick.RemoveListener(OnStartGameClick);
        }

        if (_leaveLobbyButton != null)
        {
            _leaveLobbyButton.onClick.RemoveListener(OnLeaveLobbyClick);
        }

        if (_copyLobbyCodeButton != null)
        {
            _copyLobbyCodeButton.onClick.RemoveListener(OnCopyLobbyCodeClick);
        }
    }

    private void OnZooClick()
    {
        _onZooClick?.Invoke();
    }

    private void OnGYMServerClick()
    {
        _wasConnected = false;

        SetLobbyPanelVisible(true);

        _onGYMHostClick?.Invoke();
    }

    private void OnGYMClientClick()
    {
        if (_hostProductUserIdInputField == null)
        {
            Debug.LogError("Lobby code input field is not assigned.");
            return;
        }

        string lobbyCode = _hostProductUserIdInputField.text.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(lobbyCode))
        {
            Debug.LogWarning("Lobby code is empty.");
            return;
        }

        _wasConnected = false;

        SetLobbyPanelVisible(true);

        _onGYMClientClick?.Invoke(lobbyCode);
    }

    private void OnStartGameClick()
    {
        _onStartGameClick?.Invoke();
    }

    private void OnLeaveLobbyClick()
    {
        SetLobbyPanelVisible(false);

        _wasConnected = false;

        _onLeaveLobbyClick?.Invoke();
    }

    private void OnCopyLobbyCodeClick()
    {
        if (string.IsNullOrEmpty(_currentLobbyCode))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = _currentLobbyCode;
    }
}
