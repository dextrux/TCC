using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem.Datas
{
    public enum AudioCategory
    {
        Sfx,
        Voice,
        Ambient,
        Music
    }

    [CreateAssetMenu(menuName = "Audio/Audio Event", fileName = "New AudioEvent")]
    public class AudioEvent : ScriptableObject
    {
        [Header("Clips")]
        [SerializeField] private AudioClip[] clips;

        [Header("Playback")]
        [SerializeField] private AudioCategory category = AudioCategory.Sfx;
        [SerializeField, Range(0, 1)] private float volume = 1f;
        [SerializeField, Range(0f, 360f)] private float spread = 80f;
        [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);
        [SerializeField] private bool loop;

        [Header("3D Settings")]
        [SerializeField] private bool is3D = true;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 30f;

        [Tooltip("X = Distancia | Y = Intensidade")]
        [SerializeField] private AnimationCurve rolloffCurve = AnimationCurve.Linear(0, 1, 1, 0);

        public AudioCategory Category => category;
        public float Volume => volume;
        public float Spread => spread;
        public bool Loop => loop;
        public bool Is3D => is3D;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public AnimationCurve RolloffCurve => rolloffCurve;

        public AudioClip GetClip()
        {
            if (clips != null && clips.Length != 0)
                return clips.Length == 1 ? clips[0] : clips[Random.Range(0, clips.Length)];

#if UNITY_EDITOR
            Debug.LogWarning($"[AudioEvent] '{name}' nao possui clipes configurados.");
#endif
            return null;

        }

        public float GetRandomPitch() => Random.Range(pitchRange.x, pitchRange.y);
    }
}
