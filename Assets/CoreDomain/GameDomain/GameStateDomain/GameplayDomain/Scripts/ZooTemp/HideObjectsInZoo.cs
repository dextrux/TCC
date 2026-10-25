using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.ZooTemp
{
    public class HideObjectsInZoo : MonoBehaviour
    {
        public void Start()
        {
            var audioUi = FindAnyObjectByType(typeof(TestAudio)) as TestAudio;
            audioUi!.transform.parent.gameObject.SetActive(false);
        }
    }
}
