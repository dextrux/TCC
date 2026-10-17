using Mirror;
using UnityEngine;

public struct PlayerMovementInputMessage : NetworkMessage
{
    public Vector2 MovementInput;
}