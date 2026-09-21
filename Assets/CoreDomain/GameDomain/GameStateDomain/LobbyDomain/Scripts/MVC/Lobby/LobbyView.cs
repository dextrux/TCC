using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    [SerializeField] private Button _zooButton;
    [SerializeField] private Button _GYMClientButton;
    [SerializeField] private Button _GYMServerButton;
    [SerializeField] private InputField _hostProductUserIdInputField;

    private Action _onZooClick;
    private Action _onGYMHostClick;
    private Action<string> _onGYMClientClick;

    public void SetUp(Action onZooClick, Action onGYMHostClick, Action<string> onGYMClientClick)
    {
        _onZooClick = onZooClick;
        _onGYMHostClick = onGYMHostClick;
        _onGYMClientClick = onGYMClientClick;

        _zooButton.onClick.AddListener(OnZooClick);
        _GYMServerButton.onClick.AddListener(OnGYMServerClick);
        _GYMClientButton.onClick.AddListener(OnGYMClientClick);
    }

    private void OnDestroy()
    {
        _zooButton.onClick.RemoveListener(OnZooClick);
        _GYMServerButton.onClick.RemoveListener(OnGYMServerClick);
        _GYMClientButton.onClick.RemoveListener(OnGYMClientClick);
    }

    private void OnZooClick()
    {
        _onZooClick?.Invoke();
    }

    private void OnGYMServerClick()
    {
        _onGYMHostClick?.Invoke();
    }

    private void OnGYMClientClick()
    {
        if (_hostProductUserIdInputField == null)
        {
            Debug.LogError("Host Product User ID input field is not assigned.");
            return;
        }

        string hostProductUserId = _hostProductUserIdInputField.text.Trim();
        _onGYMClientClick?.Invoke(hostProductUserId);
    }
}