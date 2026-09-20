using System;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas
{
    public enum AudioEventId
    {
        None = 0,
        FootstepConcrete,
        FootstepGrass,
        DoorOpen,
        DoorClose,
        Torch
    }
    
    [CreateAssetMenu(menuName = "Audio/Audio Event Registry", fileName = "AudioEventRegistry")]
    public class AudioEventRegistry : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public AudioEventId id;
            public AudioEvent audioEvent;
        }

        [SerializeField] private Entry[] events;

        public AudioEvent GetById(AudioEventId id)
        {
            foreach (var entry in events)
            {
                if (entry.id == id)
                    return entry.audioEvent;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"[AudioEventRegistry] Nenhum AudioEvent encontrado para Id '{id}'.");
#endif
            return null;
        }
    }
}
