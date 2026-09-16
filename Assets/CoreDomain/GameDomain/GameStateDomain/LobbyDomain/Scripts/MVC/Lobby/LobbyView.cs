using CoreDomain.Scripts.Services.SceneService;
using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    [SerializeField] private Button _zooButton;

    private Action<ScenesType> OnClickZooButton;
    public void SetUp(Action<ScenesType> OnZooButonCLick) {
        OnClickZooButton = OnZooButonCLick;
        _zooButton.onClick.AddListener(OnZooClick);
    }

    private void OnZooClick() {
        OnClickZooButton.Invoke(ScenesType.ZooScene);
    }
}
