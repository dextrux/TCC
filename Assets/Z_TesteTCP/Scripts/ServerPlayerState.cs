using UnityEngine;

public class ServerPlayerState
{
    public int Id;
    public string Name = "Player";

    public GameObject Object;
    public CharacterController Controller;
    public FpsServerPlayerBody Body;

    public float MoveX;
    public float MoveZ;

    public float PendingLookX;
    public float PendingLookY;

    public bool PendingJump;
    public bool PendingFire;

    public float Yaw;
    public float Pitch;
    public float VerticalVelocity;

    public int Health = 100;

    public float LastFireTime = -100f;
    public int LastInputSequence = -1;
}
