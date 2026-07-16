using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central gameplay input contract. Logitech F310 and other modern Windows pads
/// should use XInput mode so Unity exposes the physical A/B/X/Y buttons through
/// the semantic Gamepad controls used here.
/// </summary>
public static class MineInput
{
    public const string ControllerRunButton = "A";
    public const string ControllerJumpButton = "B";
    public const string ControllerInteractButton = "X";
    public const string ControllerPotionButton = "Y";
    public const string ControllerPauseButton = "START";
    public const string ControllerHomeButton = "BACK";
    public const float MovementDeadZone = .2f;

    public static bool HasSemanticGamepad => Gamepad.current != null;
    public static string ConnectedGamepadName => Gamepad.current == null
        ? string.Empty
        : Gamepad.current.displayName;

    public static float Horizontal
    {
        get
        {
            float keyboard = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) keyboard -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) keyboard += 1f;
            if (Mathf.Abs(keyboard) > MovementDeadZone) return Mathf.Clamp(keyboard, -1f, 1f);

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float stick = gamepad.leftStick.ReadValue().x;
                float dpad = gamepad.dpad.ReadValue().x;
                float controller = Mathf.Abs(dpad) > Mathf.Abs(stick) ? dpad : stick;
                return Mathf.Abs(controller) >= MovementDeadZone ? controller : 0f;
            }

            // Generic/legacy joystick fallback. Face buttons are treated as the
            // conventional XInput indices; semantic A/B/X/Y behavior requires X mode.
            float legacy = Input.GetAxisRaw("Horizontal");
            return Mathf.Abs(legacy) >= MovementDeadZone ? Mathf.Clamp(legacy, -1f, 1f) : 0f;
        }
    }

    public static bool RunHeld =>
        Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
        (Gamepad.current?.buttonSouth.isPressed ?? LegacyButtonHeld(KeyCode.JoystickButton0));

    public static bool JumpPressed =>
        Input.GetKeyDown(KeyCode.Space) ||
        (Gamepad.current?.buttonEast.wasPressedThisFrame ?? LegacyButtonDown(KeyCode.JoystickButton1));

    public static bool JumpHeld =>
        Input.GetKey(KeyCode.Space) ||
        (Gamepad.current?.buttonEast.isPressed ?? LegacyButtonHeld(KeyCode.JoystickButton1));

    public static bool JumpReleased =>
        Input.GetKeyUp(KeyCode.Space) ||
        (Gamepad.current?.buttonEast.wasReleasedThisFrame ?? LegacyButtonUp(KeyCode.JoystickButton1));

    public static bool InteractPressed =>
        Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
        (Gamepad.current?.buttonWest.wasPressedThisFrame ?? LegacyButtonDown(KeyCode.JoystickButton2));

    public static bool PotionPressed =>
        Input.GetKeyDown(KeyCode.H) ||
        (Gamepad.current?.buttonNorth.wasPressedThisFrame ?? LegacyButtonDown(KeyCode.JoystickButton3));

    public static bool PausePressed =>
        Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) ||
        (Gamepad.current?.startButton.wasPressedThisFrame ?? LegacyButtonDown(KeyCode.JoystickButton7));

    public static bool HomePressed =>
        Input.GetKeyDown(KeyCode.Backspace) ||
        (Gamepad.current?.selectButton.wasPressedThisFrame ?? LegacyButtonDown(KeyCode.JoystickButton6));

    public static bool ConfirmPressed =>
        Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
        Input.GetKeyDown(KeyCode.Space) ||
        (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? LegacyButtonDown(KeyCode.JoystickButton0));

    private static bool LegacyButtonDown(KeyCode key) =>
        Gamepad.current == null && Input.GetKeyDown(key);

    private static bool LegacyButtonHeld(KeyCode key) =>
        Gamepad.current == null && Input.GetKey(key);

    private static bool LegacyButtonUp(KeyCode key) =>
        Gamepad.current == null && Input.GetKeyUp(key);
}
