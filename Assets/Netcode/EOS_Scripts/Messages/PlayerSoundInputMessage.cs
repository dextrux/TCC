
using Mirror;

public struct PlayerSoundInputMessage : NetworkMessage
{
    public int SoundId;
    public float Loudness;
}
