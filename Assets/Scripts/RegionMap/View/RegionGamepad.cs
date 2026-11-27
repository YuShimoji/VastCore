using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Vastcore.RegionMap
{
    internal static class RegionGamepad
    {
        internal static Vector2 KeyboardMove(Keyboard keyboard)
        {
            if (keyboard == null) return Vector2.zero;
            return new Vector2(
                Axis(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
                    keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed),
                Axis(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
                    keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed));
        }

        internal static float TriggerAxis(Gamepad gamepad)
            => gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();

        internal static string Label(ButtonControl button)
            => button.shortDisplayName switch
            {
                "Cross" => "×",
                "Circle" => "○",
                "Square" => "□",
                "Triangle" => "△",
                _ => button.shortDisplayName ?? button.displayName,
            };

        private static float Axis(bool negative, bool positive)
            => (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}
