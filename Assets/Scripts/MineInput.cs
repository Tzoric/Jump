using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum MineButtonAction
{
    Run,
    Jump,
    Interact,
    Potion,
    Pause,
    Home
}

public readonly struct MineRebindResult
{
    public MineButtonAction Action { get; }
    public MineButtonAction? SwappedAction { get; }
    public string DisplayName { get; }

    public MineRebindResult(MineButtonAction action, MineButtonAction? swappedAction, string displayName)
    {
        Action = action;
        SwappedAction = swappedAction;
        DisplayName = displayName;
    }
}

/// <summary>
/// Central keyboard/controller input contract. Controller button overrides are
/// stored per controller model; keyboard and menu navigation stay fixed so the
/// controls screen can always be reached.
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
    public const int BindableActionCount = 6;

    private const string ActionResourceName = "MineControllerActions";
    private const string ActionMapName = "MineButtons";
    private const string ProfileKeyPrefix = "Jump.ControllerBindings.v1.";

    private static readonly MineButtonAction[] BindableActions =
    {
        MineButtonAction.Run,
        MineButtonAction.Jump,
        MineButtonAction.Interact,
        MineButtonAction.Potion,
        MineButtonAction.Pause,
        MineButtonAction.Home
    };

    private static InputActionAsset actionsAsset;
    private static InputActionMap buttonMap;
    private static InputAction[] buttonActions;
    private static InputDevice activeController;
    private static string activeProfilePreferenceKey;
    private static InputActionRebindingExtensions.RebindingOperation activeRebind;
    private static int lastControllerActivityFrame = -1;

    public static event Action BindingsChanged;

    public static bool HasSemanticGamepad => Gamepad.current != null;
    public static bool HasController
    {
        get
        {
            EnsureInitialized();
            return activeController != null;
        }
    }

    public static bool IsRebinding => activeRebind != null;
    public static string ConnectedGamepadName => Gamepad.current == null
        ? string.Empty
        : Gamepad.current.displayName;
    public static string ConnectedControllerName
    {
        get
        {
            EnsureInitialized();
            return activeController == null ? "NO CONTROLLER DETECTED" : activeController.displayName;
        }
    }

    public static float Horizontal
    {
        get
        {
            EnsureInitialized();
            RefreshActiveControllerFromActivity();
            float keyboard = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) keyboard -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) keyboard += 1f;
            if (Mathf.Abs(keyboard) > MovementDeadZone) return Mathf.Clamp(keyboard, -1f, 1f);

            if (activeController is Gamepad gamepad)
            {
                float stick = gamepad.leftStick.ReadValue().x;
                float dpad = gamepad.dpad.ReadValue().x;
                float controller = Mathf.Abs(dpad) > Mathf.Abs(stick) ? dpad : stick;
                return Mathf.Abs(controller) >= MovementDeadZone ? controller : 0f;
            }

            if (activeController is Joystick joystick)
            {
                float stick = joystick.stick.ReadValue().x;
                float hat = joystick.hatswitch == null ? 0f : joystick.hatswitch.ReadValue().x;
                float controller = Mathf.Abs(hat) > Mathf.Abs(stick) ? hat : stick;
                return Mathf.Abs(controller) >= MovementDeadZone ? controller : 0f;
            }

            float legacy = Input.GetAxisRaw("Horizontal");
            return Mathf.Abs(legacy) >= MovementDeadZone ? Mathf.Clamp(legacy, -1f, 1f) : 0f;
        }
    }

    public static Vector2 ControllerMoveVector
    {
        get
        {
            EnsureInitialized();
            RefreshActiveControllerFromActivity();
            if (activeController is Gamepad gamepad)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                Vector2 dpad = gamepad.dpad.ReadValue();
                return dpad.sqrMagnitude > stick.sqrMagnitude ? dpad : stick;
            }

            if (activeController is Joystick joystick)
            {
                Vector2 stick = joystick.stick.ReadValue();
                Vector2 hat = joystick.hatswitch == null
                    ? Vector2.zero
                    : joystick.hatswitch.ReadValue();
                return hat.sqrMagnitude > stick.sqrMagnitude ? hat : stick;
            }

            return Vector2.zero;
        }
    }

    public static bool RunHeld =>
        Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
        ReadAction(MineButtonAction.Run).IsPressed();

    public static bool JumpPressed =>
        Input.GetKeyDown(KeyCode.Space) || ReadAction(MineButtonAction.Jump).WasPressedThisFrame();

    public static bool JumpHeld =>
        Input.GetKey(KeyCode.Space) || ReadAction(MineButtonAction.Jump).IsPressed();

    public static bool JumpReleased =>
        Input.GetKeyUp(KeyCode.Space) || ReadAction(MineButtonAction.Jump).WasReleasedThisFrame();

    public static bool InteractPressed =>
        Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
        ReadAction(MineButtonAction.Interact).WasPressedThisFrame();

    // Parachute deployment deliberately shares the contextual Interact control,
    // not Jump. Up/W and the mapped controller Interact button therefore open
    // the canopy in a descent while Space/B remains available for the landing.
    public static bool ParachuteHeld =>
        Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) ||
        ReadAction(MineButtonAction.Interact).IsPressed();

    public static bool PotionPressed =>
        Input.GetKeyDown(KeyCode.H) || ReadAction(MineButtonAction.Potion).WasPressedThisFrame();

    public static bool PausePressed =>
        Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) ||
        ReadAction(MineButtonAction.Pause).WasPressedThisFrame();

    public static bool HomePressed =>
        Input.GetKeyDown(KeyCode.Backspace) || ReadAction(MineButtonAction.Home).WasPressedThisFrame();

    // UI Submit remains fixed independently of gameplay remapping so menus and
    // the Game Over restart screen can never become unreachable.
    public static bool ConfirmPressed =>
        Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
        Input.GetKeyDown(KeyCode.Space) || FixedControllerSubmitPressed();

    public static MineButtonAction[] GetBindableActions() =>
        (MineButtonAction[])BindableActions.Clone();

    public static string GetActionName(MineButtonAction action) => action switch
    {
        MineButtonAction.Run => "RUN",
        MineButtonAction.Jump => "JUMP",
        MineButtonAction.Interact => "INTERACT / PARACHUTE",
        MineButtonAction.Potion => "HEALTH POTION",
        MineButtonAction.Pause => "PAUSE",
        MineButtonAction.Home => "RETURN TO SHOP",
        _ => action.ToString().ToUpperInvariant()
    };

    public static string GetDefaultControllerBindingPath(MineButtonAction action)
    {
        InputAction inputAction = ReadAction(action);
        return inputAction.bindings[0].path;
    }

    public static string GetControllerBindingPath(MineButtonAction action)
    {
        InputAction inputAction = ReadAction(action);
        return inputAction.bindings[0].effectivePath;
    }

    public static string GetControllerBindingDisplayName(MineButtonAction action)
    {
        InputAction inputAction = ReadAction(action);
        string display = inputAction.GetBindingDisplayString(0,
            InputBinding.DisplayStringOptions.DontIncludeInteractions);
        if (inputAction.bindings[0].effectivePath.EndsWith("/select", StringComparison.OrdinalIgnoreCase))
            return "BACK / SELECT";
        return string.IsNullOrWhiteSpace(display) ? "UNASSIGNED" : display.ToUpperInvariant();
    }

    /// <summary>
    /// On the Controls page, deliberate activity selects the single-player pad.
    /// Gameplay uses the same meaningful-activity rule so a second connected pad
    /// can take over without an idle device stealing control.
    /// </summary>
    public static bool SelectMostRecentlyUsedController()
    {
        EnsureInitialized();
        InputDevice candidate = FindMeaningfullyActiveController();
        if (candidate == null && IsUsableController(activeController)) return false;
        candidate ??= FindMostRecentlyUsedController();
        if (candidate == activeController) return false;
        SwitchController(candidate);
        return true;
    }

    public static bool AnyControllerButtonPressed
    {
        get
        {
            EnsureInitialized();
            if (activeController == null) return false;
            foreach (InputControl control in activeController.allControls)
            {
                if (control is ButtonControl button && button.isPressed) return true;
            }
            return false;
        }
    }

    public static bool BeginInteractiveRebind(MineButtonAction action,
        Action<MineRebindResult> onComplete, Action onCancel)
    {
        EnsureInitialized();
        if (activeRebind != null || activeController == null) return false;

        InputAction inputAction = buttonActions[(int)action];
        string previousPath = inputAction.bindings[0].effectivePath;
        buttonMap.Disable();
        try
        {
            string deviceControls = activeController.path + "/*";
            activeRebind = inputAction.PerformInteractiveRebinding(0)
                .WithControlsHavingToMatchPath(deviceControls)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Gamepad>/dpad/*")
                .WithControlsExcluding("<Gamepad>/leftStick/*")
                .WithControlsExcluding("<Gamepad>/rightStick/*")
                .WithControlsExcluding("<Joystick>/stick/*")
                .WithControlsExcluding("<Joystick>/hat/*")
                .WithControlsExcluding("<Joystick>/hatswitch/*")
                .WithTimeout(12f)
                .OnComplete(operation => CompleteInteractiveRebind(
                    operation, action, previousPath, onComplete))
                .OnCancel(operation => CancelInteractiveRebind(operation, onCancel));
            activeRebind.Start();
            return true;
        }
        catch (Exception exception)
        {
            activeRebind?.Dispose();
            activeRebind = null;
            buttonMap.Enable();
            Debug.LogException(exception);
            return false;
        }
    }

    public static void CancelInteractiveRebind()
    {
        activeRebind?.Cancel();
    }

    public static void ResetCurrentControllerBindings()
    {
        EnsureInitialized();
        CancelInteractiveRebind();
        bool wasEnabled = buttonMap.enabled;
        if (wasEnabled) buttonMap.Disable();
        actionsAsset.RemoveAllBindingOverrides();
        ApplyControllerDefaultBindings(activeController);
        if (!string.IsNullOrEmpty(activeProfilePreferenceKey))
        {
            PlayerPrefs.DeleteKey(activeProfilePreferenceKey);
            PlayerPrefs.Save();
        }
        if (wasEnabled) buttonMap.Enable();
        BindingsChanged?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        activeRebind?.Dispose();
        activeRebind = null;
        if (actionsAsset != null)
        {
            actionsAsset.Disable();
            if (Application.isPlaying) UnityEngine.Object.Destroy(actionsAsset);
            else UnityEngine.Object.DestroyImmediate(actionsAsset);
        }
        actionsAsset = null;
        buttonMap = null;
        buttonActions = null;
        activeController = null;
        activeProfilePreferenceKey = null;
        lastControllerActivityFrame = -1;
        BindingsChanged = null;
    }

    private static void EnsureInitialized()
    {
        if (actionsAsset != null) return;

        InputActionAsset template = Resources.Load<InputActionAsset>(ActionResourceName);
        if (template == null)
        {
            throw new InvalidOperationException(
                $"Required controller action resource '{ActionResourceName}' is missing.");
        }

        actionsAsset = UnityEngine.Object.Instantiate(template);
        actionsAsset.name = ActionResourceName + " Runtime";
        buttonMap = actionsAsset.FindActionMap(ActionMapName, true);
        buttonActions = new InputAction[BindableActionCount];
        foreach (MineButtonAction action in BindableActions)
        {
            InputAction inputAction = buttonMap.FindAction(action.ToString(), true);
            if (inputAction.bindings.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Controller action '{action}' must have exactly one rebindable controller slot.");
            }
            buttonActions[(int)action] = inputAction;
        }

        InputSystem.onDeviceChange += OnDeviceChange;
        SwitchController(FindMostRecentlyUsedController(), false);
        buttonMap.Enable();
    }

    private static InputAction ReadAction(MineButtonAction action)
    {
        EnsureInitialized();
        RefreshActiveControllerFromActivity();
        return buttonActions[(int)action];
    }

    private static void CompleteInteractiveRebind(
        InputActionRebindingExtensions.RebindingOperation operation,
        MineButtonAction action, string previousPath, Action<MineRebindResult> onComplete)
    {
        InputControl selectedControl = operation.selectedControl;
        MineButtonAction? swapped = null;
        if (selectedControl != null)
        {
            foreach (MineButtonAction otherAction in BindableActions)
            {
                if (otherAction == action) continue;
                InputAction other = buttonActions[(int)otherAction];
                if (!InputControlPath.Matches(other.bindings[0].effectivePath, selectedControl)) continue;
                other.ApplyBindingOverride(0, previousPath);
                swapped = otherAction;
                break;
            }
        }

        SaveActiveProfile();
        string display = GetControllerBindingDisplayNameWithoutInitialization(action);
        activeRebind = null;
        operation.Dispose();
        buttonMap.Enable();
        BindingsChanged?.Invoke();
        onComplete?.Invoke(new MineRebindResult(action, swapped, display));
    }

    private static void CancelInteractiveRebind(
        InputActionRebindingExtensions.RebindingOperation operation, Action onCancel)
    {
        activeRebind = null;
        operation.Dispose();
        buttonMap?.Enable();
        onCancel?.Invoke();
    }

    private static string GetControllerBindingDisplayNameWithoutInitialization(MineButtonAction action)
    {
        InputAction inputAction = buttonActions[(int)action];
        string display = inputAction.GetBindingDisplayString(0,
            InputBinding.DisplayStringOptions.DontIncludeInteractions);
        if (inputAction.bindings[0].effectivePath.EndsWith("/select", StringComparison.OrdinalIgnoreCase))
            return "BACK / SELECT";
        return string.IsNullOrWhiteSpace(display) ? "UNASSIGNED" : display.ToUpperInvariant();
    }

    private static void SaveActiveProfile()
    {
        if (string.IsNullOrEmpty(activeProfilePreferenceKey)) return;
        string json = actionsAsset.SaveBindingOverridesAsJson();
        if (string.IsNullOrEmpty(json)) PlayerPrefs.DeleteKey(activeProfilePreferenceKey);
        else PlayerPrefs.SetString(activeProfilePreferenceKey, json);
        PlayerPrefs.Save();
    }

    private static void SwitchController(InputDevice controller, bool notify = true)
    {
        if (actionsAsset == null) return;
        if (activeRebind != null) CancelInteractiveRebind();
        bool wasEnabled = buttonMap != null && buttonMap.enabled;
        if (wasEnabled) buttonMap.Disable();

        activeController = controller;
        actionsAsset.devices = controller == null ? null : new[] { controller };
        activeProfilePreferenceKey = controller == null ? null : BuildProfilePreferenceKey(controller);
        actionsAsset.RemoveAllBindingOverrides();

        bool loadedSavedProfile = false;
        if (!string.IsNullOrEmpty(activeProfilePreferenceKey) &&
            PlayerPrefs.HasKey(activeProfilePreferenceKey))
        {
            string json = PlayerPrefs.GetString(activeProfilePreferenceKey, string.Empty);
            try
            {
                actionsAsset.LoadBindingOverridesFromJson(json);
                loadedSavedProfile = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Ignoring invalid saved controller mapping: {exception.Message}");
                actionsAsset.RemoveAllBindingOverrides();
                PlayerPrefs.DeleteKey(activeProfilePreferenceKey);
                PlayerPrefs.Save();
            }
        }

        if (!loadedSavedProfile) ApplyControllerDefaultBindings(controller);

        if (wasEnabled) buttonMap.Enable();
        if (notify) BindingsChanged?.Invoke();
    }

    private static InputDevice FindMostRecentlyUsedController()
    {
        // Prefer semantic XInput-style gamepads when both a gamepad and a noisy
        // generic USB joystick are connected. Meaningful player activity can
        // still switch the selected device immediately afterward.
        if (IsUsableController(Gamepad.current)) return Gamepad.current;

        InputDevice best = null;
        double bestUpdate = double.MinValue;
        foreach (InputDevice device in InputSystem.devices)
        {
            if (!IsUsableController(device)) continue;
            if (best == null || device.lastUpdateTime > bestUpdate)
            {
                best = device;
                bestUpdate = device.lastUpdateTime;
            }
        }

        if (Joystick.current != null && Joystick.current.lastUpdateTime >= bestUpdate) return Joystick.current;
        return best;
    }

    private static void RefreshActiveControllerFromActivity()
    {
        if (!Application.isPlaying || activeRebind != null || lastControllerActivityFrame == Time.frameCount)
            return;

        lastControllerActivityFrame = Time.frameCount;
        InputDevice candidate = FindMeaningfullyActiveController();
        if (candidate != null && candidate != activeController) SwitchController(candidate);
    }

    private static InputDevice FindMeaningfullyActiveController()
    {
        InputDevice best = null;
        double bestUpdate = double.MinValue;
        foreach (InputDevice device in InputSystem.devices)
        {
            if (!IsUsableController(device) || !HasMeaningfulActivity(device)) continue;
            if (best == null || device.lastUpdateTime > bestUpdate)
            {
                best = device;
                bestUpdate = device.lastUpdateTime;
            }
        }
        return best;
    }

    private static bool HasMeaningfulActivity(InputDevice device)
    {
        Vector2 movement = Vector2.zero;
        if (device is Gamepad gamepad)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            Vector2 dpad = gamepad.dpad.ReadValue();
            movement = dpad.sqrMagnitude > stick.sqrMagnitude ? dpad : stick;
        }
        else if (device is Joystick joystick)
        {
            Vector2 stick = joystick.stick.ReadValue();
            Vector2 hat = joystick.hatswitch == null ? Vector2.zero : joystick.hatswitch.ReadValue();
            movement = hat.sqrMagnitude > stick.sqrMagnitude ? hat : stick;
        }

        if (movement.sqrMagnitude >= .35f * .35f) return true;
        foreach (InputControl control in device.allControls)
        {
            if (control is ButtonControl button && !button.synthetic && button.isPressed) return true;
        }
        return false;
    }

    private static void ApplyControllerDefaultBindings(InputDevice controller)
    {
        if (controller is not Joystick joystick || buttonActions == null) return;

        // Common DirectInput ordering used by Logitech F-series and many retro
        // USB pads: X=0, A=1, B=2, Y=3, Back=8, Start=9. We bind the actual
        // controls exposed by this specific device, then the mapping screen can
        // save any model-specific corrections without affecting other pads.
        var preferredButtons = new Dictionary<MineButtonAction, int>
        {
            { MineButtonAction.Run, 1 },
            { MineButtonAction.Jump, 2 },
            { MineButtonAction.Interact, 0 },
            { MineButtonAction.Potion, 3 },
            { MineButtonAction.Pause, 9 },
            { MineButtonAction.Home, 8 }
        };
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (MineButtonAction action in BindableActions)
        {
            ButtonControl button = FindJoystickButton(joystick, preferredButtons[action], usedPaths);
            if (button == null) continue;
            buttonActions[(int)action].ApplyBindingOverride(0, button.path);
            usedPaths.Add(button.path);
        }
    }

    private static ButtonControl FindJoystickButton(Joystick joystick, int preferredIndex,
        ISet<string> usedPaths)
    {
        string preferredName = preferredIndex == 0 ? "trigger" : $"button{preferredIndex}";
        foreach (InputControl control in joystick.allControls)
        {
            if (control is ButtonControl button && !button.synthetic && button.parent == joystick &&
                string.Equals(button.name, preferredName, StringComparison.OrdinalIgnoreCase) &&
                !usedPaths.Contains(button.path))
            {
                return button;
            }
        }

        var directButtons = new List<ButtonControl>();
        foreach (InputControl control in joystick.allControls)
        {
            if (control is ButtonControl button && !button.synthetic && button.parent == joystick &&
                !usedPaths.Contains(button.path))
            {
                directButtons.Add(button);
            }
        }
        directButtons.Sort((left, right) => string.Compare(left.name, right.name,
            StringComparison.OrdinalIgnoreCase));
        return preferredIndex >= 0 && preferredIndex < directButtons.Count
            ? directButtons[preferredIndex]
            : null;
    }

    private static bool IsSupportedController(InputDevice device) =>
        device is Gamepad || device is Joystick;

    private static bool IsUsableController(InputDevice device) =>
        IsSupportedController(device) && device.added && device.enabled;

    private static string BuildProfilePreferenceKey(InputDevice device)
    {
        var description = device.description;
        string fingerprint = string.Join("|",
            device.layout,
            description.interfaceName ?? string.Empty,
            description.deviceClass ?? string.Empty,
            description.manufacturer ?? string.Empty,
            description.product ?? device.displayName ?? string.Empty,
            description.capabilities ?? string.Empty).ToLowerInvariant();

        uint hash = 2166136261u;
        for (int index = 0; index < fingerprint.Length; index++)
        {
            hash ^= fingerprint[index];
            hash *= 16777619u;
        }
        return ProfileKeyPrefix + hash.ToString("X8");
    }

    private static bool FixedControllerSubmitPressed()
    {
        EnsureInitialized();
        if (activeController is Gamepad gamepad) return gamepad.buttonSouth.wasPressedThisFrame;
        if (activeController is Joystick joystick) return joystick.trigger.wasPressedThisFrame;
        return Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!IsSupportedController(device)) return;
        if (device == activeController &&
            (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected ||
             change == InputDeviceChange.Disabled))
        {
            CancelInteractiveRebind();
            SwitchController(FindMostRecentlyUsedController());
        }
        else if (activeController == null &&
                 (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected ||
                  change == InputDeviceChange.Enabled))
        {
            SwitchController(device);
        }
        else if (device == activeController && change == InputDeviceChange.ConfigurationChanged)
        {
            SwitchController(device);
        }
    }
}
