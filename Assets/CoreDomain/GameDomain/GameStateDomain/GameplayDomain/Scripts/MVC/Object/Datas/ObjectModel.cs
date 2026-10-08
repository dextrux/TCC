using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas
{
    public class ObjectModel
    {
        public Vector3 Velocity { get; private set; }
        public bool IsThrown { get; private set; }
        
        public bool IsOpen { get; private set; }
        public void ToggleOpen() => IsOpen = !IsOpen;

        public void Throw(Vector3 velocity)
        {
            Velocity = velocity;
            IsThrown = true;
        }

        public void ApplyGravity(float gravity, float deltaTime) =>
            Velocity += Vector3.up * (gravity * deltaTime);

        public void Stop()
        {
            Velocity = Vector3.zero;
            IsThrown = false;
        }
    }
}