#if UNITY_EDITOR
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy;
using UnityEditor;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.Editor
{
    [CustomEditor(typeof(FieldOfView))]
    public class FieldOfViewEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var fov = (FieldOfView)target;

            var eye = fov.EyePosition;
            var floor = new Vector3(eye.x, fov.FloorY, eye.z);

            var forward = Vector3.ProjectOnPlane(fov.EyeForward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            var left = Quaternion.AngleAxis(-fov.angle / 2f, Vector3.up) * forward;
            var right = Quaternion.AngleAxis(fov.angle / 2f, Vector3.up) * forward;

            // Alcance total (circulo) na altura dos olhos e no chao
            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawWireArc(eye, Vector3.up, Vector3.forward, 360, fov.radius);
            Handles.DrawWireArc(floor, Vector3.up, Vector3.forward, 360, fov.radius);

            // Cone: arco no alto (olhos) e no chao, ligados pelas bordas e pelo centro
            Handles.color = Color.yellow;
            Handles.DrawWireArc(eye, Vector3.up, left, fov.angle, fov.radius);
            Handles.DrawWireArc(floor, Vector3.up, left, fov.angle, fov.radius);

            DrawEdge(eye, floor, left, fov.radius);
            DrawEdge(eye, floor, right, fov.radius);
            Handles.DrawLine(eye, eye + forward * fov.radius);

            // Linha ate o alvo visto no ultimo scan (o scan so roda em Play, no servidor)
            if (fov.canSeePlayer && fov.VisibleTarget != null)
            {
                Handles.color = Color.pink;
                Handles.DrawLine(eye, fov.VisibleTarget.position);
            }
        }

        // Borda do cone: olho -> ponta na altura do olho -> descida ate o chao -> volta ao pe
        private static void DrawEdge(Vector3 eye, Vector3 floor, Vector3 direction, float radius)
        {
            var topEnd = eye + direction * radius;
            var bottomEnd = floor + direction * radius;

            Handles.DrawLine(eye, topEnd);
            Handles.DrawLine(topEnd, bottomEnd);
            Handles.DrawLine(floor, bottomEnd);
        }
    }
}
#endif