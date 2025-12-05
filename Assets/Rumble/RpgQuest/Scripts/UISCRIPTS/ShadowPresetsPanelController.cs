using UnityEngine;
using UnityEngine;
using UnityEngine.UIElements;

public class ShadowPresetsPanelController : MonoBehaviour
{
    [Tooltip("If left empty, the script will FindObjectOfType at runtime.")]
    public URPShadowPresetSwitcher presetSwitcher;

    [Header("Optional: set a label to reflect the active preset")]
    public string performanceLabel = "Performance";
    public string balancedLabel = "Balanced";
    public string cinematicLabel = "Cinematic";

    private Label _status;

    void Awake()
    {
        // Resolve switcher if not wired
        if (presetSwitcher == null)
            presetSwitcher = FindObjectOfType<URPShadowPresetSwitcher>(includeInactive: true);
    }

    void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;

        var btnPerf = root.Q<Button>("BtnPerformance");
        var btnBal  = root.Q<Button>("BtnBalanced");
        var btnCin  = root.Q<Button>("BtnCinematic");
        _status     = root.Q<Label>("LblStatus");

        if (btnPerf != null) btnPerf.clicked += OnPerformanceClicked;
        if (btnBal  != null) btnBal.clicked  += OnBalancedClicked;
        if (btnCin  != null) btnCin.clicked  += OnCinematicClicked;

        // Update label once on open (assume Balanced default)
        SetStatus(balancedLabel);
    }

    void OnDisable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var btnPerf = root.Q<Button>("BtnPerformance");
        var btnBal  = root.Q<Button>("BtnBalanced");
        var btnCin  = root.Q<Button>("BtnCinematic");

        if (btnPerf != null) btnPerf.clicked -= OnPerformanceClicked;
        if (btnBal  != null) btnBal.clicked  -= OnBalancedClicked;
        if (btnCin  != null) btnCin.clicked  -= OnCinematicClicked;
    }

    void OnPerformanceClicked()
    {
        if (presetSwitcher == null) return;
        presetSwitcher.ApplyPerformance();
        SetStatus(performanceLabel);
    }

    void OnBalancedClicked()
    {
        if (presetSwitcher == null) return;
        presetSwitcher.ApplyBalanced();
        SetStatus(balancedLabel);
    }

    void OnCinematicClicked()
    {
        if (presetSwitcher == null) return;
        presetSwitcher.ApplyCinematic();
        SetStatus(cinematicLabel);
    }

    void SetStatus(string name)
    {
        if (_status != null) _status.text = $"Current: {name}";
    }
}
