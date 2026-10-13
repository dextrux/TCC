using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using UnityEngine;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces
{
    public interface IAudioManager
    {
        void PlayAtPosition(AudioEvent audioEvent, Vector3 position, float volumeMultiplier = 1f);
        void PlayUI(AudioEvent audioEvent);
        void ReportSignal(AudioSignal audioSignal);
        void SetCategoryVolume(AudioCategory category, float volume);
        void StopAll(AudioCategory category);
    }
}