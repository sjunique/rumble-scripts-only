using UnityEngine;

public class AutoPilotHotkeysOnly : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to auto-find at runtime.")]
    public AutoPilotNavigator target;

    [Header("Keys")]
    public KeyCode startKey  = KeyCode.F5;
    public KeyCode returnKey = KeyCode.F6;
    public KeyCode stopKey   = KeyCode.Backspace;

    [Header("Options")]
    public bool logActions = true;
    public bool autoFindEveryFrame = true; // keep trying until a craft appears

    void Update()
    {
        if (!target && autoFindEveryFrame)
              target = FindObjectOfType<AutoPilotNavigator>();

        // if (!target) return;

        // // --- Legacy Input (Old/Both) ---
        // if (Input.GetKeyDown(startKey))
        // {
        //     target.StartAutoPilotForward();
        //     if (logActions) Debug.Log("[AP] Start forward");
        // }
        // if (Input.GetKeyDown(returnKey))
        // {
        //     target.StartAutoPilotReturn();
        //     if (logActions) Debug.Log("[AP] Start return");
        // }
        // if (Input.GetKeyDown(stopKey))
        // {
        //     target.StopAutoPilot();
        //     if (logActions) Debug.Log("[AP] Stop");
        // }

        // --- New Input System (optional) ---
        // If your project is "New Input System only", uncomment this block and
        // ensure com.unity.inputsystem is installed. Also comment out the legacy block above.
      
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.f5Key.wasPressedThisFrame)               { target.StartAutoPilotForward(); if (logActions) Debug.Log("[AP] Start forward"); }
            if (kb.f6Key.wasPressedThisFrame)               { target.StartAutoPilotReturn();  if (logActions) Debug.Log("[AP] Start return");  }
            if (kb.backspaceKey.wasPressedThisFrame)        { target.StopAutoPilot();         if (logActions) Debug.Log("[AP] Stop");          }
        }
        #endif
    
    }
}