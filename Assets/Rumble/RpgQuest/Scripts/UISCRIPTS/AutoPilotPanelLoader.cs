using System;
using UnityEngine;
using UnityEngine.UIElements;

public class AutoPilotPanelLoader : MonoBehaviour
{
    [Header("Optional pre-placed refs")]
    [SerializeField] private UIDocument uiDoc;            // leave null if none
    [SerializeField] private PanelSettings panelSettings; // optional; else created at runtime

    [Header("Target (optional)")]
    public AutoPilotNavigator target;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.F9;

    [Header("Hotkeys")]
    public bool enableHotkeys = true;
    public KeyCode startKey = KeyCode.F5;
    public KeyCode returnKey = KeyCode.F6;
    public KeyCode stopKey = KeyCode.Backspace;

    // UI refs
    private VisualElement root;
    private VisualElement _hotkeyScope;
    private Button bStart, bReturn, bStop;
    private Label status;

    // State
    private bool shown;
    private bool _hotkeysEnabled;
    private bool _panelVisible;
    private CursorLockMode _prevLock;
    private bool _prevVisible;

    void Start()
    {
        EnsureUIDocument();
        BuildAndBind();
        Show(); // start visible; change to Hide() if you prefer hidden by default
    }

    // -- Setup ----------------------------------------------------------------

    void EnsureUIDocument()
    {
        if (!uiDoc) uiDoc = GetComponent<UIDocument>();

        if (!uiDoc)
        {
            // Find an existing UIDocument named "AP_UITK_Panel" (no LINQ)
            UIDocument[] docs = FindObjectsOfType<UIDocument>(true);
            for (int i = 0; i < docs.Length; i++)
            {
                if (docs[i] != null && docs[i].name == "AP_UITK_Panel")
                {
                    uiDoc = docs[i];
                    break;
                }
            }
        }

        if (!uiDoc)
        {
            var host = new GameObject("AP_UITK_Panel");
            host.transform.SetParent(transform, false);
            uiDoc = host.AddComponent<UIDocument>();
        }

        if (!uiDoc.panelSettings)
        {
            var ps = panelSettings ? panelSettings : ScriptableObject.CreateInstance<PanelSettings>();
            ps.sortingOrder = 5000;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            uiDoc.panelSettings = ps;
        }
    }

    void BuildAndBind()
    {
        // Load assets under Resources/UI/
        var vta = Resources.Load<VisualTreeAsset>("UI/AutoPilotPanel");
        var uss = Resources.Load<StyleSheet>("UI/AutoPilotPanel");
        if (!vta || !uss) { Debug.LogError("[AP_UI] Missing UXML/USS at Resources/UI/"); return; }

        // Clear & rebuild
        var rve = uiDoc.rootVisualElement;
        rve.style.flexGrow = 1f;
        rve.StretchToParentSize();
        rve.Clear();

        // Explicitly clone UXML (avoid visualTreeAsset timing)
        var tree = vta.Instantiate();
        tree.style.flexGrow = 1f;
        tree.StretchToParentSize();
        rve.Add(tree);

        // Apply USS
        tree.styleSheets.Add(uss);

        root = tree;

        // Bind elements and events
        BindAndWire(root);

        // Wait for geometry before bringing to front / pointer test
        var panel = root.Q<VisualElement>("ap-panel") ?? root;
        panel.RegisterCallback<GeometryChangedEvent>(OnApPanelGeometryReady);

        // Install UI hotkeys (work even when panel has focus)
        InstallUiHotkeys();
    }

    void BindAndWire(VisualElement r)
    {
        r.style.display = DisplayStyle.Flex;
        r.style.flexGrow = 1f;
        r.StretchToParentSize();

        // Query
        var panel = r.Q<VisualElement>("ap-panel") ?? r;
        status = r.Q<Label>("ap-status");
        bStart = r.Q<Button>("ap-start");
        bReturn = r.Q<Button>("ap-return");
        bStop = r.Q<Button>("ap-stop");


        panel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);

        // Make pickable & focusable
        panel.pickingMode = PickingMode.Position;
        SetupButton(bStart, StartAP, "[AP_UI] Click ap-start");
        SetupButton(bReturn, ReturnAP, "[AP_UI] Click ap-return");
        SetupButton(bStop, StopAP, "[AP_UI] Click ap-stop");



    }

    private void OnPanelPointerUp(PointerUpEvent e)
    {
        // Pointer in panel coordinates
        Vector2 p = e.position;

        // If the pointer is within a button's world rect, fire its action.
        if (bStart != null && bStart.worldBound.Contains(p) && bStart.resolvedStyle.display != DisplayStyle.None && bStart.enabledSelf) { StartAP(); e.StopPropagation(); return; }
        if (bReturn != null && bReturn.worldBound.Contains(p) && bReturn.resolvedStyle.display != DisplayStyle.None && bReturn.enabledSelf) { ReturnAP(); e.StopPropagation(); return; }
        if (bStop != null && bStop.worldBound.Contains(p) && bStop.resolvedStyle.display != DisplayStyle.None && bStop.enabledSelf) { StopAP(); e.StopPropagation(); return; }
    }



    void SetupButton(Button b, Action onClick, string debugLabel)
    {
        if (b == null) return;
        b.style.display = DisplayStyle.Flex;
        b.pickingMode = PickingMode.Position;
        b.focusable = true;
        b.tabIndex = 0;

        // Clean bind
        b.clicked -= onClick;
        b.clicked += onClick;

        // Extra safety for some UITK builds
        b.UnregisterCallback<PointerUpEvent>(OnPointerUpNoop);
        b.RegisterCallback<PointerUpEvent>(_ => onClick());
        b.RegisterCallback<ClickEvent>(_ => Debug.Log(debugLabel));
    }

    private void OnPointerUpNoop(PointerUpEvent ev) { }

    void OnApPanelGeometryReady(GeometryChangedEvent e)
    {
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        var panel = root.Q<VisualElement>("ap-panel") ?? root;
        panel.pickingMode = PickingMode.Position;


        //    var panel = (VisualElement)e.target;
        panel.UnregisterCallback<GeometryChangedEvent>(OnApPanelGeometryReady);

        panel.pickingMode = PickingMode.Position;
        panel.BringToFront();
        MoveToTop(panel);

        Debug.Log($"[AP_UI] Panel laid out: {panel.layout}");

        foreach (var b in new[] { bStart, bReturn, bStop })
        {
            if (b == null) continue;
            b.pickingMode = PickingMode.Position;
            b.focusable = true;
            b.tabIndex = 0;
        }


        // Pointer probe: confirms events reach the panel
        panel.RegisterCallback<PointerDownEvent>(ev =>
            Debug.Log($"[AP_UI] PointerDown on panel at {ev.position}, target: {ev.target}"));
    }

    static void MoveToTop(VisualElement ve)
    {
        var parent = ve?.parent; if (parent == null) return;
        if (parent.IndexOf(ve) != parent.childCount - 1)
        {
            ve.RemoveFromHierarchy();
            parent.Add(ve);
        }
    }

    // -- Hotkeys --------------------------------------------------------------

    void InstallUiHotkeys()
    {
        _hotkeyScope = root.Q<VisualElement>("ap-panel") ?? root;
        _hotkeyScope.focusable = true;
        _hotkeyScope.RegisterCallback<KeyDownEvent>(OnUiKeyDown, TrickleDown.TrickleDown);
    }

    void OnUiKeyDown(KeyDownEvent e)
    {
        switch (e.keyCode)
        {
            case KeyCode.S: StartAP(); e.StopPropagation(); break;
            case KeyCode.R: ReturnAP(); e.StopPropagation(); break;
            case KeyCode.X: StopAP(); e.StopPropagation(); break;
        }
    }

    // -- Show / Hide ----------------------------------------------------------

    public void Show()
    {
        _panelVisible = true;
        _hotkeysEnabled = true;
        shown = true;

        _prevLock = UnityEngine.Cursor.lockState; _prevVisible = UnityEngine.Cursor.visible;
        UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true;

        if (root != null) root.style.display = DisplayStyle.Flex;
        (root?.Q<VisualElement>("ap-panel") ?? root)?.Focus();
    }

    public void Hide()
    {
        _panelVisible = false;
        _hotkeysEnabled = false;
        shown = false;

        if (root != null) root.style.display = DisplayStyle.None;
        UnityEngine.Cursor.lockState = _prevLock; UnityEngine.Cursor.visible = _prevVisible;
    }

    // -- Actions --------------------------------------------------------------

    void StartAP() { if (EnsureTarget()) target.StartAutoPilotForward(); }
    void ReturnAP() { if (EnsureTarget()) target.StartAutoPilotReturn(); }
    void StopAP() { if (EnsureTarget()) target.StopAutoPilot(); }

    bool EnsureTarget()
    {
        if (!target) target = FindObjectOfType<AutoPilotNavigator>();
        return target != null;
    }

    // -- Update loop: toggle & fallback hotkeys -------------------------------

    void Update()
    {
        // Toggle panel
        if (Input.GetKeyDown(toggleKey))
        {
            if (!shown) Show(); else Hide();
        }

        // Fallback hotkeys (work regardless of focus / action maps) while visible
        if (_hotkeysEnabled && shown && EnsureTarget() && enableHotkeys)
        {
            if (Input.GetKeyDown(startKey)) target.StartAutoPilotForward();
            if (Input.GetKeyDown(returnKey)) target.StartAutoPilotReturn();
            if (Input.GetKeyDown(stopKey)) target.StopAutoPilot();

            // Optional S/R/X convenience:
            if (Input.GetKeyDown(KeyCode.S)) StartAP();
            if (Input.GetKeyDown(KeyCode.R)) ReturnAP();
            if (Input.GetKeyDown(KeyCode.X)) StopAP();
        }

        // Simple status + button enable
        if (shown && status != null && target != null)
        {
            string phase = "Idle";
            bool active = false;
            try { phase = target.PhaseLabel; } catch { }
            try { active = target.IsAutopilotActive; } catch { }

            status.text = $"AP: {phase}";
            if (bStart != null) bStart.SetEnabled(!active);
            if (bReturn != null) bReturn.SetEnabled(!active);
            if (bStop != null) bStop.SetEnabled(active);
        }
    }
}
