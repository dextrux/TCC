using CoreDomain.GameDomain.Scripts.AudioSystem;
using Player.View;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.ZooTemp
{
    public class HideObjectsInZoo : MonoBehaviour
    {
        public void Start()
        {
            var audioUi = FindAnyObjectByType(typeof(AudioUi)) as AudioUi;
            var player = FindAnyObjectByType(typeof(FPSPlayerView)) as FPSPlayerView;
        
            player!.transform.GetChild(0).gameObject.SetActive(false);
            player!.transform.GetChild(2).gameObject.SetActive(false);
            audioUi!.transform.parent.gameObject.SetActive(false);
        }
    }
}
