using UnityEngine;

namespace Player.Model {
    [CreateAssetMenu(fileName = "FPSPlayerConfiguration", menuName = "Player/FPS Player Configuration")]
    public class FPSPlayerConfigurationSO : ScriptableObject {
        [Header("Movement")]
        [field: SerializeField]
        public float MoveSpeed { get; private set; } = 5f;

        [field: SerializeField]
        public float Gravity { get; private set; } = -20f;

        [field: SerializeField]
        public float JumpForce { get; private set; } = 8f;

        [Header("Look")]
        [field: SerializeField]
        public float LookSensitivity { get; private set; } = 0.1f;

        [field: SerializeField]
        public float MinPitch { get; private set; } = -80f;

        [field: SerializeField]
        public float MaxPitch { get; private set; } = 80f;
    }
}