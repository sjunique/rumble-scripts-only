using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
[DefaultExecutionOrder(10000)]
public class AutoPilotPanel_UITK : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to auto-find at runtime.")]
    public AutoPilotNavigator target;

    [Header("Panel")]
    public Vector2 panelSize = new Vector2(220, 128);
    [Range(0f, 1f)] public float panelOpacity = 0.35f;
    public float padding = 10f;
    public float spacing = 6f;
    public int fontSize = 14;

    [Header("Hotkeys")]
    public bool enableHotkeys = true;
    public KeyCode startKey = KeyCode.F5;
    public KeyCode returnKey = KeyCode.F6;
    public KeyCode stopKey = KeyCode.Backspace;

    private UIDocument uiDoc;
    private PanelSettings panelSettings;
    private VisualElement panel, column;
    private Label status;
    private Button btnStart, btnReturn, btnStop;
    private IVisualElementScheduledItem tick;

    void OnEnable()
    {
        // Ensure UIDocument (no assets needed)
        uiDoc = GetComponent<UIDocument>();
        if (!uiDoc) uiDoc = gameObject.AddComponent<UIDocument>();

        // Ensure PanelSettings so Unity won't prompt
        if (!uiDoc.panelSettings)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.targetDisplay = 0;
            panelSettings.sortingOrder = 2000; // on top
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;
            panelSettings.clearDepthStencil = true;
            panelSettings.clearColor = false;
            uiDoc.panelSettings = panelSettings;
        }

        // Build UI in code (no UXML)
        var root = uiDoc.rootVisualElement;
        root.style.flexGrow = 1;

        panel = new VisualElement { name = "AP_Panel" };
        panel.style.position = Position.Absolute;
        panel.style.top = 12;
        panel.style.right = 12;
        panel.style.width = panelSize.x;
        panel.style.minHeight = panelSize.y;
        panel.style.paddingLeft = padding;
        panel.style.paddingRight = padding;
        panel.style.paddingTop = padding;
        panel.style.paddingBottom = padding;

        // Brighter background
      //  panel.style.backgroundColor = new Color(0.22f, 0.24f, 0.28f, Mathf.Clamp01(panelOpacity));
        panel.style.backgroundColor = new Color(0.22f, 0.24f, 0.28f, 0.45f);

        panel.style.borderTopLeftRadius = 10;
        panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10;
        panel.style.borderBottomRightRadius = 10;

        column = new VisualElement();
        column.style.flexDirection = FlexDirection.Column;
        #if UNITY_2023_2_OR_NEWER
      //  column.style.gap = spacing;          // use 'gap' if available
        #endif
        panel.Add(column);

     // Status
status = new Label("AP: Idle");
status.style.unityTextAlign = TextAnchor.MiddleCenter;
status.style.fontSize = fontSize + 2;
status.style.unityFontStyleAndWeight = FontStyle.Bold;

// ✅ Force a runtime font + white color
status.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("Arial.ttf"));
status.style.color = Color.white;

AddWithSpacing(status);
        btnStart  = MakeBtn("Start ▶");
        btnReturn = MakeBtn("Return ↩");
        btnStop   = MakeBtn("Stop ⏹");

        AddWithSpacing(btnStart);
        AddWithSpacing(btnReturn);
        AddWithSpacing(btnStop, isLast: true);

        root.Add(panel);
root.schedule.Execute(() =>
{
    ApplyButtonTextColor(btnStart,  Color.white);
    ApplyButtonTextColor(btnReturn, Color.white);
    ApplyButtonTextColor(btnStop,   Color.white);
    status.style.color = Color.white;
}).StartingIn(10);

        // Wire actions (safe guards)
        btnStart.clicked  += () => { if (EnsureTarget()) target.StartAutoPilotForward(); };
        btnReturn.clicked += () => { if (EnsureTarget()) target.StartAutoPilotReturn();  };
        btnStop.clicked   += () => { if (EnsureTarget()) target.StopAutoPilot();         };

        // Schedule UI updates (~60 fps)
        tick = root.schedule.Execute(UpdateUI).Every(16);

        // Make sure hotkeys reach us
        root.focusable = true;
        panel.focusable = true;
        root.schedule.Execute(() => panel.Focus()).StartingIn(50);

        // UITK key events (work with New Input System too)
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (!enableHotkeys) return;
            if (!EnsureTarget()) return;

            if (evt.keyCode == startKey)  { target.StartAutoPilotForward(); evt.StopPropagation(); }
            if (evt.keyCode == returnKey) { target.StartAutoPilotReturn();  evt.StopPropagation(); }
            if (evt.keyCode == stopKey)   { target.StopAutoPilot();         evt.StopPropagation(); }
        });
    }

    void Update()
    {
        // Legacy input fallback (if project uses Old/Both)
        if (!enableHotkeys) return;
        if (!EnsureTarget()) return;

        if (Input.GetKeyDown(startKey))  target.StartAutoPilotForward();
        if (Input.GetKeyDown(returnKey)) target.StartAutoPilotReturn();
        if (Input.GetKeyDown(stopKey))   target.StopAutoPilot();
    }

    void OnDisable()
    {
        tick?.Pause();
        uiDoc?.rootVisualElement?.Clear();
    }

    // ---- Helpers ----
    void UpdateUI()
    {
        if (!EnsureTarget())
        {
            status.text = "AP: (no craft)";
            SetInteractable(false, false, false);
            return;
        }

        // Optional properties on your AutoPilotNavigator; fall back if absent
        string phase = "Active";
        bool active = true;
        try { phase = target.PhaseLabel; } catch {}
        try { active = target.IsAutopilotActive; } catch {}

        status.text = $"AP: {phase}";
        SetInteractable(!active, !active, active);
    }

// Add inside AutoPilotPanel_UITK
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
        ve.style.marginBottom = isLast ? 0 : spacing; // emulate 'gap' on older Unity
        #endif
        column.Add(ve);
    }

    void SetInteractable(bool start, bool ret, bool stop)
    {
        btnStart?.SetEnabled(start);
        btnReturn?.SetEnabled(ret);
        btnStop?.SetEnabled(stop);
    }

    bool EnsureTarget()
    {
        if (!target) target = FindObjectOfType<AutoPilotNavigator>();
        return target != null;
    }
}
