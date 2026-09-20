using CoreDomain.Scripts.Services.SceneService;
using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    [SerializeField] private Button _zooButton;

    private Action<string> OnClickZooButton;
    public void SetUp(Action<string> OnZooButonCLick) {
        OnClickZooButton = OnZooButonCLick;
        _zooButton.onClick.AddListener(OnZooClick);
    }

    private void OnZooClick() {
        OnClickZooButton.Invoke("ZooScene");
    }
}
