using UnityEngine;

namespace Player.Input {
    public interface IPlayerInput {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }

        bool JumpPressed { get; }
    }
}