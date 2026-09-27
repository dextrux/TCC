using UnityEngine;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Datas
{
    [CreateAssetMenu(menuName = "Audio/Audio Event Registry", fileName = "AudioEventRegistry")]
    public class AudioEventRegistry : ScriptableObject
    {
        [SerializeField] private AudioEventCategoryRegistry[] subRegistries;

        public AudioEvent GetById(AudioEventId id)
        {
            foreach (var subRegistry in subRegistries)
            {
                if (subRegistry == null) continue;

                var audioEvent = subRegistry.GetById(id);
                if (audioEvent != null)
                    return audioEvent;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"[AudioEventRegistry] Nenhum AudioEvent encontrado para Id '{id}' em nenhuma categoria.");
#endif
            return null;
        }

#if UNITY_EDITOR
        // Ajuda a pegar erro de configuracao no Editor: mesmo Id cadastrado
        // em duas categorias diferentes ao mesmo tempo (ambiguidade).
        [ContextMenu("Validar Ids duplicados entre categorias")]
        private void ValidateDuplicateIds()
        {
            for (var i = 0; i < subRegistries.Length; i++)
            {
                for (var j = i + 1; j < subRegistries.Length; j++)
                {
                    var a = subRegistries[i];
                    var b = subRegistries[j];
                    if (a == null || b == null) continue;

                    foreach (AudioEventId id in System.Enum.GetValues(typeof(AudioEventId)))
                    {
                        if (a.Contains(id) && b.Contains(id))
                        {
                            Debug.LogWarning($"[AudioEventRegistry] Id '{id}' esta duplicado em '{a.CategoryName}' e '{b.CategoryName}'.");
                        }
                    }
                }
            }
        }
#endif
    }
}