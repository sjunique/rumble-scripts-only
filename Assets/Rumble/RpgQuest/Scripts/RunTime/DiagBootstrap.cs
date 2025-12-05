using UnityEngine;
// DiagBootstrap.cs
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class DiagBootstrap : MonoBehaviour
{
    [Header("Optional explicit settings (overrides Resources lookup)")]
    public DiagSettings settings;

    [Header("Hotkeys (Editor only)")]
    public KeyCode toggleEnableKey = KeyCode.F10;
    public KeyCode cycleLevelKey   = KeyCode.F10;

    void Awake()
    {
        if (settings == null)
            settings = Resources.Load<DiagSettings>("DiagSettings"); // Resources/DiagSettings.asset

        if (settings == null)
        {
            // Safe defaults if no asset found
            Diag.Enabled = true;
            Diag.Level   = DiagLevel.Info;
        }
        else
        {
            Diag.ApplySettings(settings);
        }

        DontDestroyOnLoad(gameObject);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(toggleEnableKey))
        {
            Diag.Enabled = !Diag.Enabled;
            Debug.Log($"[DiagBootstrap] Diagnostics {(Diag.Enabled ? "ENABLED" : "DISABLED")}");
        }

        if (Input.GetKeyDown(cycleLevelKey))
        {
            var next = (int)Diag.Level + 1;
            if (next > (int)DiagLevel.Verbose) next = (int)DiagLevel.Quiet;
            Diag.Level = (DiagLevel)next;
            Debug.Log($"[DiagBootstrap] Diag level → {Diag.Level}");
        }
    }
#endif
}
