using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(10000)]
public class AutoPilotHUD : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("If empty, the HUD will try to find one every frame until it succeeds.")]
    public AutoPilotNavigator target;

    [Header("Appearance")]
    public Vector2 panelSize = new Vector2(220, 132);
    public Vector2 padding = new Vector2(10, 10);
    public int fontSize = 16;
    public float spacing = 8f;
    [Range(0f,1f)] public float panelOpacity = 0.65f;

    // runtime refs
    Canvas canvas;
    RectTransform panel;
    Text statusText;
    Button btnStart, btnReturn, btnStop;

    bool _initialized;
    const string kLog = "[AutoPilotHUD] ";

    void Start()
    {
        TryInitUI();
    }

    void Update()
    {
        // If init failed in Start (rare), retry once per frame until it works
        if (!_initialized)
        {
            TryInitUI();
            return;
        }

        // Target may spawn later (e.g., addressables, scene additive, pooling)
        if (!target)
        {
            target = FindObjectOfType<AutoPilotNavigator>();
            if (!target)
            {
                // No craft yet → keep HUD disabled but visible with message
                if (statusText) statusText.text = "AP: (no craft)";
                SetButtonsInteractable(false, false, false);
                return;
            }
        }

        // Update status safely
        if (statusText)
        {
            // These accessors are optional; fall back if not present
            string phase = "Active";
            bool active = true;

            try { phase = target.PhaseLabel; } catch { phase = "Active"; }
            try { active = target.IsAutopilotActive; } catch { active = true; }

            statusText.text = $"AP: {phase}";
            SetButtonsInteractable(!active, !active, active);
        }
    }

    // ----------------- UI creation with guards -----------------
    void TryInitUI()
    {
        try
        {
            EnsureEventSystem();

            var goCanvas = new GameObject("AutoPilotHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(goCanvas);
            canvas = goCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = goCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Panel
            var goPanel = new GameObject("Panel", typeof(Image));
            goPanel.transform.SetParent(goCanvas.transform, false);
            panel = goPanel.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(1, 1);
            panel.anchorMax = new Vector2(1, 1);
            panel.pivot = new Vector2(1, 1);
            panel.sizeDelta = panelSize;
            panel.anchoredPosition = new Vector2(-padding.x, -padding.y);

            var panelImg = goPanel.GetComponent<Image>();
            if (panelImg) panelImg.color = new Color(0.08f, 0.09f, 0.1f, panelOpacity);

            // Layout
            var layout = goPanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = spacing;
            layout.padding = new RectOffset(12, 12, 12, 12);

            // Status text
            statusText = CreateText(goPanel.transform, "AP: Idle", fontSize, FontStyle.Bold);
            if (!statusText) Debug.LogWarning(kLog + "Failed to create statusText");

            // Buttons
            btnStart  = CreateButton(goPanel.transform, "Start ▶");
            btnReturn = CreateButton(goPanel.transform, "Return ↩");
            btnStop   = CreateButton(goPanel.transform, "Stop ⏹");

            if (btnStart)  btnStart.onClick.AddListener(() => { if (EnsureTarget()) target.StartAutoPilotForward(); });
            if (btnReturn) btnReturn.onClick.AddListener(() => { if (EnsureTarget()) target.StartAutoPilotReturn();  });
            if (btnStop)   btnStop.onClick.AddListener(() => { if (EnsureTarget()) target.StopAutoPilot();           });

            // Initial interactivity
            SetButtonsInteractable(true, true, false);

            // Hotkeys helper
            var hk = gameObject.GetComponent<AutoPilotHotkeys>();
            if (!hk) hk = gameObject.AddComponent<AutoPilotHotkeys>();
            hk.Init(this);

            _initialized = true;
            Debug.Log(kLog + "HUD initialized.");
        }
        catch (System.Exception ex)
        {
            _initialized = false;
            Debug.LogError(kLog + "Init failed: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    void EnsureEventSystem()
    {
        if (!FindObjectOfType<EventSystem>())
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
            Debug.Log(kLog + "Created EventSystem (StandaloneInputModule).");
        }
    }

    void SetButtonsInteractable(bool start, bool ret, bool stop)
    {
        if (btnStart)  btnStart.interactable  = start;
        if (btnReturn) btnReturn.interactable = ret;
        if (btnStop)   btnStop.interactable   = stop;
    }

    bool EnsureTarget()
    {
        if (!target) target = FindObjectOfType<AutoPilotNavigator>();
        if (!target)
            Debug.LogWarning(kLog + "No AutoPilotNavigator found in scene.");
        return target;
    }

    // --- UI builders (defensive) ---
    static Text CreateText(Transform parent, string text, int size, FontStyle style)
    {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        if (!t) return null;

        t.text = text;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // safe fallback
        t.fontSize = size;
        t.fontStyle = style;
        t.color = new Color(0.9f, 0.95f, 1f, 1f);

        var rt = go.GetComponent<RectTransform>();
        if (rt) rt.sizeDelta = new Vector2(0, size + 8);
        return t;
    }

    static Button CreateButton(Transform parent, string label)
    {
        var go = new GameObject(label, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        if (img) img.color = new Color(0.18f, 0.2f, 0.24f, 0.95f);

        var btn = go.GetComponent<Button>();
        if (!btn) return null;

        var colors = btn.colors;
        colors.highlightedColor = new Color(0.26f, 0.29f, 0.34f, 1f);
        colors.pressedColor     = new Color(0.16f, 0.18f, 0.22f, 1f);
        colors.selectedColor    = colors.highlightedColor;
        btn.colors = colors;

        var txt = CreateText(go.transform, label, 16, FontStyle.Normal);
        if (txt) txt.alignment = TextAnchor.MiddleCenter;

        var rt = go.GetComponent<RectTransform>();
        if (rt) rt.sizeDelta = new Vector2(0, 32);

        return btn;
    }

    // Small hotkey helper
    private class AutoPilotHotkeys : MonoBehaviour
    {
        AutoPilotHUD hud;
        public void Init(AutoPilotHUD owner) { hud = owner; }

        void Update()
        {
            if (!hud || !hud.enabled) return;
            if (Input.GetKeyDown(KeyCode.F5))        hud.btnStart?.onClick.Invoke();
            if (Input.GetKeyDown(KeyCode.F6))        hud.btnReturn?.onClick.Invoke();
            if (Input.GetKeyDown(KeyCode.Backspace)) hud.btnStop?.onClick.Invoke();
        }
    }
}
