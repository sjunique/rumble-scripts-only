using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental; // safe to include; some versions ignore

[DefaultExecutionOrder(10000)]
public class AutoPilotHUD_UITK : MonoBehaviour
{
    [Header("Target")]
    public AutoPilotNavigator target; // leave empty; it will lazy-find

    [Header("Appearance")]
    public Vector2 panelSize = new Vector2(220, 140);
    public float panelOpacity = 0.7f;
    public float padding = 10f;
    public float spacing = 6f;
    public int fontSize = 14;

    private UIDocument uiDoc;
    private PanelSettings panelSettings; // created at runtime
    private VisualElement panel, column;
    private Label status;
    private Button startBtn, returnBtn, stopBtn;
    private IVisualElementScheduledItem tick;

    void OnEnable()
    {
        // Ensure a UIDocument exists on this GameObject
        uiDoc = GetComponent<UIDocument>();
        if (!uiDoc) uiDoc = gameObject.AddComponent<UIDocument>();

        // Ensure PanelSettings (avoid Unity prompt)
        if (!uiDoc.panelSettings)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            // Sensible defaults for overlay HUD
            panelSettings.targetDisplay = 0;
            panelSettings.sortingOrder = 1000;                // on top
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;                      // match width/height
            panelSettings.clearDepthStencil = true;
            panelSettings.clearColor = false;
            uiDoc.panelSettings = panelSettings;
        }

        // No visualTreeAsset (we’ll build in code)
        uiDoc.visualTreeAsset = null;

        var root = uiDoc.rootVisualElement;
        root.style.flexGrow = 1;

        // ---- Panel ----
        panel = new VisualElement { name = "AutoPilotPanel" };
        panel.style.position = Position.Absolute;
        panel.style.top = 12;
        panel.style.right = 12;
        panel.style.width = panelSize.x;
        panel.style.minHeight = panelSize.y;
        panel.style.paddingLeft = padding;
        panel.style.paddingRight = padding;
        panel.style.paddingTop = padding;
        panel.style.paddingBottom = padding;
        panel.style.backgroundColor = new Color(0.16f, 0.18f, 0.22f, 0.35f);
        // panel.style.backgroundColor = new Color(0.10f, 0.11f, 0.13f, panelOpacity);
        panel.style.borderTopLeftRadius = 10;
        panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10;
        panel.style.borderBottomRightRadius = 10;

        // Column container
        column = new VisualElement();
        column.style.flexDirection = FlexDirection.Column;
#if UNITY_2023_2_OR_NEWER
      //  column.style.gap = spacing;         // use gap if supported
#endif
        panel.Add(column);

        // Status
     // Status
status = new Label("AP: Idle");
status.style.unityTextAlign = TextAnchor.MiddleCenter;
status.style.fontSize = fontSize + 2;
status.style.unityFontStyleAndWeight = FontStyle.Bold;

// ✅ Force a runtime font + white color
status.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("Arial.ttf"));
status.style.color = Color.white;

AddWithSpacing(status);


        // Buttons
        startBtn = MakeBtn("Start ▶");
        returnBtn = MakeBtn("Return ↩");
        stopBtn = MakeBtn("Stop ⏹");

        AddWithSpacing(startBtn);
        AddWithSpacing(returnBtn);
        AddWithSpacing(stopBtn, isLast: true);

        // FORCE WHITE TEXT
        status.style.color = new Color(1f, 1f, 1f, 1f);
        ApplyButtonTextColor(startBtn, Color.white);
        ApplyButtonTextColor(returnBtn, Color.white);
        ApplyButtonTextColor(stopBtn, Color.white);

        root.Add(panel);





        // Make root/panel focusable and grab focus so it receives key events
        root = uiDoc.rootVisualElement;

        // after root.Add(panel);
        root.schedule.Execute(() =>
        {
            ApplyButtonTextColor(startBtn, Color.white);
            ApplyButtonTextColor(returnBtn, Color.white);
            ApplyButtonTextColor(stopBtn, Color.white);
            status.style.color = Color.white;
        }).StartingIn(10);

        root.focusable = true;
        panel.focusable = true;
        panel.Focus();

        // Handle hotkeys via UITK (works even if legacy input is disabled)
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (EnsureTarget() == false) return;

            if (evt.keyCode == KeyCode.F5) { target.StartAutoPilotForward(); evt.StopPropagation(); }
            if (evt.keyCode == KeyCode.F6) { target.StartAutoPilotReturn(); evt.StopPropagation(); }
            if (evt.keyCode == KeyCode.Backspace) { target.StopAutoPilot(); evt.StopPropagation(); }
        });


        // Actions
        startBtn.clicked += () => { if (EnsureTarget()) target.StartAutoPilotForward(); };
        returnBtn.clicked += () => { if (EnsureTarget()) target.StartAutoPilotReturn(); };
        stopBtn.clicked += () => { if (EnsureTarget()) target.StopAutoPilot(); };

        // Scheduled UI tick (~60 fps)
        tick = root.schedule.Execute(UpdateUI).Every(16);
    }

    void OnDisable()
    {
        tick?.Pause();
        if (uiDoc && uiDoc.rootVisualElement != null)
            uiDoc.rootVisualElement.Clear();
    }
    void Update()
    {
        if (!EnsureTarget()) return;

        // Works if legacy input is enabled in Player Settings → Active Input Handling = Both or Old
        if (UnityEngine.Input.GetKeyDown(KeyCode.F5)) target.StartAutoPilotForward();
        if (UnityEngine.Input.GetKeyDown(KeyCode.F6)) target.StartAutoPilotReturn();
        if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace)) target.StopAutoPilot();
    }

    void UpdateUI()
    {
        if (!target) target = Object.FindObjectOfType<AutoPilotNavigator>();
        if (!target)
        {
            status.text = "AP: (no craft)";
            SetInteractable(false, false, false);
            return;
        }

        string phase = "Active";
        bool active = true;
        try { phase = target.PhaseLabel; } catch { }
        try { active = target.IsAutopilotActive; } catch { }

        status.text = $"AP: {phase}";
        SetInteractable(!active, !active, active);
    }

    // Add inside AutoPilotPanel_UITK


    // ---------- helpers ----------
   Button MakeBtn(string label)
{
    var b = new Button { text = label };

    // BG + border
    b.style.backgroundColor = new Color(0.20f, 0.22f, 0.26f, 0.98f);
    b.style.borderTopLeftRadius = 8;
    b.style.borderTopRightRadius = 8;
    b.style.borderBottomLeftRadius = 8;
    b.style.borderBottomRightRadius = 8;
    b.style.borderLeftWidth = 1;  b.style.borderLeftColor = new Color(0.4f,0.45f,0.52f,1f);
    b.style.borderRightWidth = 1; b.style.borderRightColor = new Color(0.4f,0.45f,0.52f,1f);
    b.style.borderTopWidth = 1;   b.style.borderTopColor = new Color(0.4f,0.45f,0.52f,1f);
    b.style.borderBottomWidth = 1;b.style.borderBottomColor = new Color(0.4f,0.45f,0.52f,1f);

    // 🔑 Force text color + font on the internal TextElement
    var te = b.Q<TextElement>();                  // << TextElement, not Label
    if (te != null)
    {
        te.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("Arial.ttf"));
        te.style.color = Color.white;
        te.style.fontSize = fontSize;
        te.style.unityTextAlign = TextAnchor.MiddleCenter;
    }

    // Hover/press feedback
    b.RegisterCallback<PointerOverEvent>(_ => b.style.backgroundColor = new Color(0.30f,0.34f,0.40f,0.98f));
    b.RegisterCallback<PointerOutEvent>(_ =>  b.style.backgroundColor = new Color(0.20f,0.22f,0.26f,0.98f));
    b.RegisterCallback<PointerDownEvent>(_ => b.style.backgroundColor = new Color(0.18f,0.20f,0.24f,0.98f));
    b.RegisterCallback<PointerUpEvent>(_ =>   b.style.backgroundColor = new Color(0.30f,0.34f,0.40f,0.98f));

    return b;
}


    void AddWithSpacing(VisualElement ve, bool isLast = false)
    {
#if !UNITY_2023_2_OR_NEWER
        ve.style.marginBottom = isLast ? 0 : spacing;  // emulate gap for older Unity
#endif
        column.Add(ve);
    }


  void ApplyButtonTextColor(Button b, Color c)
{
    var te = b.Q<TextElement>();
    if (te != null)
    {
        te.style.color = c;
        // Ensure font remains set (some versions drop it on rebuild)
        if (te.style.unityFontDefinition.keyword == StyleKeyword.Null)
            te.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("Arial.ttf"));
    }
    // Keep this for newer versions where it works:
    b.style.color = c;
}




    void SetInteractable(bool start, bool ret, bool stop)
    {
        startBtn?.SetEnabled(start);
        returnBtn?.SetEnabled(ret);
        stopBtn?.SetEnabled(stop);
    }

    bool EnsureTarget()
    {
        if (!target) target = Object.FindObjectOfType<AutoPilotNavigator>();
        if (!target)
        {
            Debug.LogWarning("[AutoPilotHUD_UITK] No AutoPilotNavigator found.");
            return false;
        }
        return true;
    }
}
