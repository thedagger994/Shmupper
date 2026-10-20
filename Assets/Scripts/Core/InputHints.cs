using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shmupper
{
    /// Tracks which control scheme the player last touched, and names its buttons.
    ///
    /// An arcade cabinet tells you what to press. Showing "LEFT MOUSE FIRE" to somebody holding a
    /// controller is worse than showing nothing, because it reads as a statement that the pad is
    /// not supported. Every on-screen prompt pulls its label from here, so the whole interface
    /// relabels itself the moment a stick moves or a key is pressed.
    public static class InputHints
    {
        public enum Scheme { KeyboardMouse, Gamepad }

        public static Scheme Active { get; private set; } = Scheme.KeyboardMouse;

        /// Raised only when the scheme actually changes, so listeners can rebuild their text
        /// rather than rewriting it every frame.
        public static event Action OnSchemeChanged;

        public static bool UsingGamepad => Active == Scheme.Gamepad;

        public static void Poll()
        {
            var pad = Gamepad.current;
            if (pad != null && PadIsActive(pad))
            {
                Set(Scheme.Gamepad);
                return;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.isPressed)
            {
                Set(Scheme.KeyboardMouse);
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed ||
                                  mouse.delta.ReadValue().sqrMagnitude > 4f))
            {
                Set(Scheme.KeyboardMouse);
            }
        }

        /// Sticks use a generous deadzone so a drifting controller left on a desk does not keep
        /// stealing the prompts back from the keyboard.
        static bool PadIsActive(Gamepad pad)
        {
            if (pad.leftStick.ReadValue().sqrMagnitude > 0.09f) return true;
            if (pad.rightStick.ReadValue().sqrMagnitude > 0.09f) return true;
            if (pad.rightTrigger.ReadValue() > 0.2f || pad.leftTrigger.ReadValue() > 0.2f) return true;
            if (pad.dpad.ReadValue().sqrMagnitude > 0.5f) return true;

            return pad.buttonSouth.isPressed || pad.buttonEast.isPressed ||
                   pad.buttonWest.isPressed || pad.buttonNorth.isPressed ||
                   pad.leftShoulder.isPressed || pad.rightShoulder.isPressed ||
                   pad.startButton.isPressed || pad.selectButton.isPressed;
        }

        static void Set(Scheme scheme)
        {
            if (Active == scheme) return;

            Active = scheme;
            OnSchemeChanged?.Invoke();
        }

        // ---------------------------------------------------------------- labels

        public static string Move    => UsingGamepad ? "LEFT STICK" : "W A S D";
        public static string Look    => UsingGamepad ? "RIGHT STICK" : "MOUSE";
        public static string Fire    => UsingGamepad ? "RIGHT TRIGGER" : "LEFT MOUSE";
        public static string Jump    => UsingGamepad ? "A" : "SPACE";
        public static string Switch  => UsingGamepad ? "BUMPERS / D-PAD" : "1 - 4 / WHEEL";
        public static string Flask   => UsingGamepad ? "X" : "F";
        public static string Start   => UsingGamepad ? "START" : "ENTER";
        public static string Credit  => UsingGamepad ? "SELECT" : "C";
        public static string Confirm => UsingGamepad ? "A" : "ENTER";
        public static string Back    => UsingGamepad ? "B" : "ESC";
        public static string Choose  => UsingGamepad ? "D-PAD" : "UP / DOWN";
    }
}
