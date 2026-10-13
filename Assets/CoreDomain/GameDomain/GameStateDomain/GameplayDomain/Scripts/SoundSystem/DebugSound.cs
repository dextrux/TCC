using System;
using System.Globalization;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using TMPro;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    public class DebugSound : MonoBehaviour
    {
        private TMP_Text _debugText;
        private static AudioEventId _audioEventId;
        private static float _atualVolume;

        private void Awake() => _debugText = GetComponent<TMP_Text>();

        private void Update()
        {
            AtualizeText();
        }
        
        private void AtualizeText()
        {
            _debugText.text = $"Nivel de ruido: {_atualVolume.ToString(CultureInfo.CurrentCulture)}" +
                              $"\tOrigem do ruido: {_audioEventId.ToString()}";
        }

        public static void GetVolume(AudioEventId id, float volume)
        {
            _audioEventId = id;
            _atualVolume = volume;
        }
    }
}
