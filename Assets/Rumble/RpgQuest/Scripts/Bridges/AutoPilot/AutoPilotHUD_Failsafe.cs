using UnityEngine;

[DefaultExecutionOrder(10000)]
public class AutoPilotHUD_Failsafe : MonoBehaviour
{
    [Header("Target")]
    public AutoPilotNavigator target;   // leave empty to auto-find

    [Header("Style & Layout")]
    public Vector2 panelSize = new Vector2(240, 140);
    public Vector2 margin = new Vector2(12, 12);
    [Range(0f, 1f)] public float bgOpacity = 0.35f;
    public int fontSize = 16;

    private Rect panelRect;
    private GUIStyle labelStyle, buttonStyle, boxStyle;

    void Awake()
    {
        // Top-right anchored panel rect
        float x = Screen.width - panelSize.x - margin.x;
        float y = margin.y;
        panelRect = new Rect(x, y, panelSize.x, panelSize.y);

        // Styles
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize + 2,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fontSize
        };
        boxStyle = new GUIStyle(GUI.skin.box);
    }

  void Update()
{
    Debug.Log($"[AutopilotController] Update called - Checking for hotkey input...");
    
    // Hotkeys always work (legacy input path)
    if (EnsureTarget())
    {
        Debug.Log($"[AutopilotController] Target ensured successfully");
        
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log($"[AutopilotController] F5 pressed - Starting auto pilot forward");
            target.StartAutoPilotForward();
        }
        
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log($"[AutopilotController] F6 pressed - Starting auto pilot return");
            target.StartAutoPilotReturn();
        }
        
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            Debug.Log($"[AutopilotController] Backspace pressed - Stopping auto pilot");
            target.StopAutoPilot();
        }
    }
    else
    {
        Debug.LogWarning($"[AutopilotController] EnsureTarget failed - No valid target found");
    }
    
    // Optional: Log when no relevant keys are pressed (can be verbose)
    // if (!Input.GetKeyDown(KeyCode.F5) && !Input.GetKeyDown(KeyCode.F6) && !Input.GetKeyDown(KeyCode.Backspace))
    // {
    //     Debug.Log($"[AutopilotController] No autopilot hotkeys pressed this frame");
    // }
}

    void OnGUI()
    {
        // Background box (semi transparent)
        var prevColor = GUI.color;
        GUI.color = new Color(0.16f, 0.18f, 0.22f, bgOpacity);
        GUI.Box(panelRect, GUIContent.none, boxStyle);
        GUI.color = prevColor;

        // Begin panel area
        GUILayout.BeginArea(panelRect);
        GUILayout.Space(4);

        // Status
        string phase = "Idle";
        bool active = false;
        if (EnsureTarget())
        {
            try { phase = target.PhaseLabel; } catch {}
            try { active = target.IsAutopilotActive; } catch {}
        }
        GUILayout.Label($"AP: {phase}", labelStyle, GUILayout.Height(28));

        GUILayout.Space(6);

        // Buttons
        GUILayout.BeginHorizontal();
        GUI.enabled = !active;
        if (GUILayout.Button("Start ▶", buttonStyle))  { if (EnsureTarget()) target.StartAutoPilotForward(); }
        if (GUILayout.Button("Return ↩", buttonStyle)) { if (EnsureTarget()) target.StartAutoPilotReturn();  }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        GUI.enabled = active;
        if (GUILayout.Button("Stop ⏹", buttonStyle)) { if (EnsureTarget()) target.StopAutoPilot(); }
        GUI.enabled = true;

        GUILayout.EndArea();

        // Allow dragging panel with mouse (optional)
        panelRect = GUI.Window(9999, panelRect, _ => {}, GUIContent.none);
    }

    bool EnsureTarget()
    {
        if (!target) target = FindObjectOfType<AutoPilotNavigator>();
        return target != null;
    }

    // Keep panel anchored to top-right when window resizes
    void OnRectTransformDimensionsChange()
    {
        float x = Screen.width - panelSize.x - margin.x;
        float y = margin.y;
        panelRect.x = x;
        panelRect.y = y;
        panelRect.width = panelSize.x;
        panelRect.height = panelSize.y;
    }
}
