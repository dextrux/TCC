using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem
{
    [Serializable]
    public struct NamedSoundEntry
    {
        public string actionName; // "open", "close", "pull", etc
        public SoundEntry sound;
    }

    [RequireComponent(typeof(SoundEmitter))]
    public class InteractableSoundEmitter : MonoBehaviour
    {
        [SerializeField] private NamedSoundEntry[] sounds;

        private SoundEmitter _soundEmitter;
        private Dictionary<string, SoundEntry> _lookup;

        private void Awake()
        {
            _soundEmitter = GetComponent<SoundEmitter>();

            _lookup = new Dictionary<string, SoundEntry>(sounds.Length);
            foreach (var entry in sounds)
                _lookup[entry.actionName] = entry.sound;
        }

        /// <summary>
        ///Chamado no script ou AnimationEvent do objeto
        /// </summary>
        public void EmitAction(string actionName)
        {
            if (!_lookup.TryGetValue(actionName, out var sound))
            {
                Debug.LogWarning($"[InteractableSoundEmitter] Acao '{actionName}' nao configurada em {name}.");
                return;
            }

            _soundEmitter.EmitSound(sound.id, sound.loudness);
        }
    }
}