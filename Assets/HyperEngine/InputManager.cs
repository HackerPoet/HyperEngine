using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.XR.Haptics;

public class InputManager {
    //Game preferences
    public static float LOOK_SENSITIVITY = 1.0f;
    public static bool INVERT_Y_AXIS = false;
    public static bool INVERT_X_AXIS = false;
    public static bool ENABLE_RUMBLE = true;

    //Singleton to hold the input action asset
    private static bool isPaused = false;
    private static InputActionAsset inputActionAsset = null;
    private static InputActionMap inputActionMap = null;
    private static InputDevice[] XRControllers = new InputDevice[2];
    private static double actionTimeAbsorbed = 0.0f;
    private static InputActionMap GetInputActionMap() {
        if (inputActionAsset == null) {
            InitializeControllers();
            inputActionAsset = Resources.Load<InputActionAsset>("Controls");
            inputActionAsset.Enable();
            inputActionMap = null;
        }
        if (inputActionMap == null) {
            inputActionMap = inputActionAsset.FindActionMap("Gameplay");
        }
        return inputActionMap;
    }

    public static bool pauseInput {
        get { return isPaused; }
        set {
            isPaused = value;
            Rumble(false);
        }
    }

    private static int GetHandIx(InputDevice device) {
        foreach (string usage in device.usages) {
            if (usage.Contains(CommonUsages.LeftHand)) {
                return 0;
            } else if (usage.Contains(CommonUsages.RightHand)) {
                return 1;
            }
        }
        return 0;
    }

    private static void InitializeControllers() {
        InputSystem.onDeviceChange += (device, change) => {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected) {
                XRControllers[GetHandIx(device)] = device;
            } else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) {
                XRControllers[GetHandIx(device)] = null;
            }
        };
    }

    public static bool GetKey(string key, bool overridePause = false) {
        bool isInteract = (key == "Interact");
        if (isPaused && !overridePause) {
            return false;
        } else if (isInteract && IsActionAbsorbed()) {
            return false;
        } else {
            InputAction action = GetInputActionMap().FindAction(key);
            if (action == null) {
                Debug.LogWarning("Unknown input key: " + key);
                return false;
            }
            return (action.phase == InputActionPhase.Started || action.phase == InputActionPhase.Performed);
        }
    }

    public static bool GetKeyDown(string key, bool overridePause = false) {
        bool isInteract = (key == "Interact");
        if (isPaused && !overridePause) {
            return false;
        } else if (isInteract && IsActionAbsorbed()) {
            return false;
        } else {
            InputAction action = GetInputActionMap().FindAction(key);
            if (action == null) {
                Debug.LogWarning("Unknown input key: " + key);
                return false;
            }
            return action.triggered;
        }
    }

    public static bool GetInteractAbsorb(float delay = 0.0f, bool overridePause = false) {
        bool triggered = GetKeyDown("Interact", overridePause);
        if (triggered) { AbsorbAction(delay); }
        return triggered;
    }

    public static bool GetSubmitAbsorb(float delay = 0.0f, bool overridePause = false) {
        if (IsActionAbsorbed()) { return false; }
        bool triggered = GetKeyDown("Submit", overridePause);
        if (!triggered) { triggered = GetKeyDown("SubmitAlt", overridePause); }
        if (triggered) { AbsorbAction(delay); }
        return triggered;
    }

    public static Vector2 GetAxes(string axis, bool overridePause = false, bool ignoreLookSensitivity = false) {
        if (isPaused && !overridePause) {
            return Vector3.zero;
        }
        InputAction inputAction = GetInputActionMap().FindAction(axis);
        if (inputAction == null) {
            Debug.LogWarning("Unknown input axis: " + axis);
            return Vector3.zero;
        }
        Vector2 result = inputAction.ReadValue<Vector2>();
        if (axis == "Look") {
            float lookSensitivity = (ignoreLookSensitivity ? 1.0f : LOOK_SENSITIVITY);
            return new Vector2(result.x * lookSensitivity * (INVERT_X_AXIS ? -1.0f : 1.0f),
                               result.y * lookSensitivity * (INVERT_Y_AXIS ? -1.0f : 1.0f));
        }
        return result;
    }

    public static float GetAxis(string axis, bool overridePause = false) {
        string xy = axis.Substring(axis.Length - 1);
        Debug.Assert(xy == "X" || xy == "Y", "Axis must specify X or Y");
        Vector2 value = GetAxes(axis.Substring(0, axis.Length - 1), overridePause);
        if (xy == "X") {
            return value.x;
        } else {
            return value.y;
        }
    }

    public static void AbsorbAction(float delay = 0.0f) {
        actionTimeAbsorbed = Time.timeAsDouble + delay;
    }
    public static bool IsActionAbsorbed() {
        return actionTimeAbsorbed >= Time.timeAsDouble;
    }

    public static void Rumble(bool enable) {
        if (enable && !ENABLE_RUMBLE) { return; }
        Gamepad gamePad = Gamepad.current;
        if (gamePad != null) {
            if (enable) {
                gamePad.SetMotorSpeeds(0.5f, 0.25f);
            } else {
                gamePad.ResetHaptics();
            }
        }
        foreach (InputDevice device in XRControllers) {
            XRControllerWithRumble hapticDevice = device as XRControllerWithRumble;
            if (hapticDevice != null) {
                //BUG: Channel 0 does not work with haptics. Unity suggested a workaround
                //     to use channel 1 instead. Fix this in a future OpenXR update.
                var rumble = SendHapticImpulseCommand.Create(1, 1.0f, enable ? 999.0f : 0.0f);
                device.ExecuteCommand(ref rumble);
            }
        }
    }
}
