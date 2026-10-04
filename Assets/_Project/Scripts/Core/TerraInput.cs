using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectTerra.Core
{
    /// <summary>
    /// Centralized bridge for Unity's new Input System (com.unity.inputsystem).
    /// Provides null-safe mouse, keyboard, and gamepad readings with backward-compatible ergonomics.
    /// </summary>
    public static class TerraInput
    {
        public static Vector2 MousePosition
        {
            get
            {
                var mouse = Mouse.current;
                return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            }
        }

        public static Vector2 MouseDelta
        {
            get
            {
                var mouse = Mouse.current;
                // Unity's legacy Input.GetAxis("Mouse X/Y") returns pixel movement scaled by ~0.1
                return mouse != null ? mouse.delta.ReadValue() * 0.1f : Vector2.zero;
            }
        }

        public static float MouseScroll
        {
            get
            {
                var mouse = Mouse.current;
                if (mouse == null) return 0f;
                float raw = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(raw) < 0.0001f) return 0f;

                // Normaliza para passos de roda (~1.0 por notch):
                // No Windows (Raw Input), 1 notch típico do mouse reporta +/-120 (ou frações de 15, 30, 60 em mouses de alta resolução).
                // Em touchpads, macOS, Linux ou mouses com delta já normalizado, reporta +/-1.0 (ou frações contínuas).
                return Mathf.Abs(raw) >= 15f ? (raw / 120f) : raw;
            }
        }

        public static bool GetMouseButton(int button)
        {
            var mouse = Mouse.current;
            if (mouse == null) return false;
            return button switch
            {
                0 => mouse.leftButton.isPressed,
                1 => mouse.rightButton.isPressed,
                2 => mouse.middleButton.isPressed,
                _ => false
            };
        }

        public static bool GetMouseButtonDown(int button)
        {
            var mouse = Mouse.current;
            if (mouse == null) return false;
            return button switch
            {
                0 => mouse.leftButton.wasPressedThisFrame,
                1 => mouse.rightButton.wasPressedThisFrame,
                2 => mouse.middleButton.wasPressedThisFrame,
                _ => false
            };
        }

        public static bool GetMouseButtonUp(int button)
        {
            var mouse = Mouse.current;
            if (mouse == null) return false;
            return button switch
            {
                0 => mouse.leftButton.wasReleasedThisFrame,
                1 => mouse.rightButton.wasReleasedThisFrame,
                2 => mouse.middleButton.wasReleasedThisFrame,
                _ => false
            };
        }

        public static float GetAxis(string axisName)
        {
            switch (axisName)
            {
                case "Mouse X":
                    return MouseDelta.x;
                case "Mouse Y":
                    return MouseDelta.y;
                case "Mouse ScrollWheel":
                    return MouseScroll;
                case "Horizontal":
                {
                    float val = 0f;
                    var kb = Keyboard.current;
                    if (kb != null)
                    {
                        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) val += 1f;
                        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) val -= 1f;
                    }
                    var gp = Gamepad.current;
                    if (gp != null && Mathf.Abs(gp.leftStick.x.ReadValue()) > Mathf.Abs(val))
                    {
                        val = gp.leftStick.x.ReadValue();
                    }
                    return val;
                }
                case "Vertical":
                {
                    float val = 0f;
                    var kb = Keyboard.current;
                    if (kb != null)
                    {
                        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) val += 1f;
                        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) val -= 1f;
                    }
                    var gp = Gamepad.current;
                    if (gp != null && Mathf.Abs(gp.leftStick.y.ReadValue()) > Mathf.Abs(val))
                    {
                        val = gp.leftStick.y.ReadValue();
                    }
                    return val;
                }
                default:
                    return 0f;
            }
        }

        public static bool GetButton(string buttonName)
        {
            if (buttonName == "Jump")
            {
                var kb = Keyboard.current;
                bool space = kb != null && kb.spaceKey.isPressed;
                var gp = Gamepad.current;
                bool south = gp != null && gp.buttonSouth.isPressed;
                return space || south;
            }
            return false;
        }

        public static bool GetButtonDown(string buttonName)
        {
            if (buttonName == "Jump")
            {
                var kb = Keyboard.current;
                bool space = kb != null && kb.spaceKey.wasPressedThisFrame;
                var gp = Gamepad.current;
                bool south = gp != null && gp.buttonSouth.wasPressedThisFrame;
                return space || south;
            }
            return false;
        }

        public static bool GetButtonUp(string buttonName)
        {
            if (buttonName == "Jump")
            {
                var kb = Keyboard.current;
                bool space = kb != null && kb.spaceKey.wasReleasedThisFrame;
                var gp = Gamepad.current;
                bool south = gp != null && gp.buttonSouth.wasReleasedThisFrame;
                return space || south;
            }
            return false;
        }

        public static bool GetKey(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && kb[key].isPressed;
        }

        public static bool GetKeyDown(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && kb[key].wasPressedThisFrame;
        }

        public static bool GetKeyUp(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && kb[key].wasReleasedThisFrame;
        }
    }
}
