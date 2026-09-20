using CoreDomain.Scripts.Services.SceneService;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    [SerializeField] private Button _zooButton;
    [SerializeField] private Button _GYMClientButton;
    [SerializeField] private Button _GYMServerButton;

    private Action<string> OnClickButton;
    public void SetUp(Action<string> OnButtonCLick) {
        OnClickButton = OnButtonCLick;
        _zooButton.onClick.AddListener(OnZooClick);
        _GYMClientButton.onClick.AddListener(OnGYMClientClick);
        _GYMServerButton.onClick.AddListener(OnGYMServerClick);
    }

    private void OnZooClick() {
        OnClickButton.Invoke("ZooScene");
    }

    private void OnGYMServerClick() {
        OnClickButton.Invoke("GYMScene");
        NetworkManager.Singleton.StartHost();
    }

    private void OnGYMClientClick() {
        OnClickButton.Invoke("GYMScene");
        NetworkManager.Singleton.StartClient();
    }
}
