using Mirror;
using UnityEngine;

public struct PlayerCameraInputMessage : NetworkMessage
{
    public Vector2 CameraInput;
}