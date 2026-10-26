using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas
{
    public static class ThrowAim
    {
        public static Vector3 GetLaunchDirection(Vector3 origin, Vector3 target, float speed, float gravity)
        {
            var toTarget = target - origin;
            var horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
            var distance = horizontal.magnitude;
            
            if (distance <= Mathf.Epsilon || speed <= Mathf.Epsilon || gravity >= 0f)
                return toTarget.sqrMagnitude > Mathf.Epsilon ? toTarget.normalized : Vector3.forward;
            
            var speedSqr = speed * speed;
            var discriminant = speedSqr * speedSqr - gravity * (gravity * distance * distance + 2f * toTarget.y * speedSqr);
            
            var angle = discriminant < 0f
                ? Mathf.Atan(speedSqr / (gravity * distance))
                : Mathf.Atan((speedSqr - Mathf.Sqrt(discriminant)) / (gravity * distance));

            return (horizontal.normalized * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)).normalized;
        }
    }
}