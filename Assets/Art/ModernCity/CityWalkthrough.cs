using UnityEngine;
using UnityEngine.InputSystem;

namespace Vastcore.ModernCity
{
    // First-person camera for the sample scene. Buildings and props work without it.
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class CityWalkthrough : MonoBehaviour
    {
        [SerializeField] private Camera view;
        private CharacterController body;
        private float pitch;
        private float fallSpeed;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            if (view == null)
                view = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                ReleaseCursor();
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                CaptureCursor();

            var gamepad = Gamepad.current;
            Vector2 walk = Vector2.zero;
            Vector2 look = Vector2.zero;
            if (gamepad != null)
            {
                walk = gamepad.leftStick.ReadValue();
                look = gamepad.rightStick.ReadValue() * (105f * Time.deltaTime);
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (keyboard != null)
                {
                    walk += new Vector2(
                        Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed),
                        Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed));
                }
                if (mouse != null)
                    look += mouse.delta.ReadValue() * 0.09f;
            }

            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -83f, 83f);
            if (view != null)
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

            walk = Vector2.ClampMagnitude(walk, 1);
            fallSpeed = body.isGrounded ? -2f : Mathf.Max(fallSpeed - 20f * Time.deltaTime, -16f);
            Vector3 velocity = 3.7f * (transform.right * walk.x + transform.forward * walk.y);
            velocity.y = fallSpeed;
            body.Move(velocity * Time.deltaTime);
        }

        private static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                ReleaseCursor();
        }

        private void OnDisable() => ReleaseCursor();

        private static void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
