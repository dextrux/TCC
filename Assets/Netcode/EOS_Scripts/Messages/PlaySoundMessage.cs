using Mirror;
using UnityEngine;

public struct PlaySoundMessage : NetworkMessage
{
    public int SoundId;
    public Vector3 Position;
    public float VolumeMultiplier;
}