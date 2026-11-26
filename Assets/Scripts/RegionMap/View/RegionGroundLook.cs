using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>地上に置いた視点を、右ドラッグでその場から見回す。</summary>
    public sealed class RegionGroundLook : MonoBehaviour
    {
        private const float DegreesPerPixel = 0.16f;
        private Vector2 _previousMouse;
        private bool _dragging;

        private void Update()
        {
            if (!Input.GetMouseButton(1))
            {
                _dragging = false;
                return;
            }

            Vector2 mouse = Input.mousePosition;
            if (_dragging)
            {
                Vector2 delta = mouse - _previousMouse;
                Vector3 angles = transform.eulerAngles;
                float pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x) - delta.y * DegreesPerPixel, -88f, 88f);
                transform.rotation = Quaternion.Euler(pitch, angles.y + delta.x * DegreesPerPixel, 0f);
            }
            _previousMouse = mouse;
            _dragging = true;
        }
    }
}
