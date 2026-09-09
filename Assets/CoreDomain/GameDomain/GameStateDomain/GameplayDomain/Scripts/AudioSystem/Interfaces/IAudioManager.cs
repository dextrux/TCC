using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Interfaces
{
    public interface IAudioManager
    {
        void PlayAtPosition(AudioEvent audioEvent, Vector3 position);
        void PlayUI(AudioEvent audioEvent);
        void ReportSignal(AudioSignal audioSignal);
        void SetCategoryVolume(AudioCategory category, float volume);
        void StopAll(AudioCategory category);
    }
}
