using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// UGUI / TMP (optional)
#if UNITY_UI || UNITY_UGUI
using UnityEngine.UI;
#endif
#if TMP_PRESENT
using TMPro;
#endif

// UI Toolkit (optional)
#if UNITY_2021_3_OR_NEWER
using UnityEngine.UIElements;
#endif

/// <summary>
/// Displays lives from LivesService on either UI Toolkit or UGUI/TMP.
/// Auto-subscribes to LivesService events if available, else falls back to polling.
/// </summary>
[DisallowMultipleComponent]
public class LivesHUDController : MonoBehaviour
{
    [Header("Optional (UGUI/TMP)")]
    [Tooltip("Assign a Text (UGUI) if you use legacy UI.")]
#if UNITY_UI || UNITY_UGUI
    public Text uguiText;
#endif
#if TMP_PRESENT
   // [Tooltip("Assign a TMP_Text if you use TextMeshPro.")]
    public TMP_Text tmpText;
#endif

#if UNITY_2021_3_OR_NEWER
    [Header("Optional (UI Toolkit)")]
   // [Tooltip("Assign UIDocument if you want to bind a Label in UI Toolkit.")]
    public UIDocument uiDocument;
   // [Tooltip("Name of the UI Toolkit label to show lives. Default: 'LivesLabel'")]
    public string uiToolkitLabelName = "LivesLabel";
    private Label _uiLabel;
#endif

    [Header("Format")]
    [Tooltip("Display as 'x / y'. If false, shows just current.")]
    public bool showMax = true;
 //   [Tooltip("Prefix to show before the number(s).")]
    public string prefix = "Lives: ";

    [Header("Polling Fallback")]
    [Tooltip("If events are missing, poll every N seconds.")]
    public float pollInterval = 0.5f;

    private object _livesService;        // LivesService.Instance
    private PropertyInfo _pCurrent, _pMax;
    private float _nextPoll;
    private int _lastCurrent = int.MinValue;
    private int _lastMax = int.MinValue;
    private bool _eventBound;

    void Awake()
    {
        ResolveLivesService();

#if UNITY_2021_3_OR_NEWER
        // Find UI Toolkit label if present
        if (uiDocument == null) uiDocument = GetComponentInChildren<UIDocument>(true);
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            _uiLabel = uiDocument.rootVisualElement.Q<Label>(uiToolkitLabelName);
            if (_uiLabel != null)
                Debug.Log($"[LivesHUD] UI Toolkit label found: '{uiToolkitLabelName}'.");
            else
                Debug.LogWarning($"[LivesHUD] UI Toolkit label '{uiToolkitLabelName}' not found. " +
                                 $"Add a Label with that name or assign UGUI/TMP instead.");
        }
#endif
        // Initial draw
        UpdateHUD(force:true);
    }

    void OnEnable()
    {
        TryBindEvents();
        _nextPoll = Time.unscaledTime + pollInterval;
    }

    void OnDisable()
    {
        TryUnbindEvents();
    }

    void Update()
    {
        // If events weren’t bound, keep it fresh via polling
        if (!_eventBound && Time.unscaledTime >= _nextPoll)
        {
            _nextPoll = Time.unscaledTime + pollInterval;
            UpdateHUD();
        }
    }

    // ─────────────────── LivesService hookup ───────────────────

    void ResolveLivesService()
    {
        // LivesService.Instance via reflection (no hard link required)
        var asm = AppDomain.CurrentDomain.GetAssemblies();
        var livesType = asm.SelectMany(a => a.GetTypes())
                           .FirstOrDefault(t => t.Name == "LivesService");
        if (livesType == null)
        {
            Debug.LogError("[LivesHUD] LivesService type not found.");
            return;
        }

        var pInstance = livesType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _livesService = pInstance?.GetValue(null);
        if (_livesService == null)
        {
            Debug.LogWarning("[LivesHUD] LivesService.Instance is null at Awake. It may initialize later.");
            return;
        }

        _pCurrent = livesType.GetProperty("CurrentLives", BindingFlags.Public | BindingFlags.Instance);
        _pMax     = livesType.GetProperty("MaxLives", BindingFlags.Public | BindingFlags.Instance);

        if (_pCurrent == null)
            Debug.LogWarning("[LivesHUD] LivesService.CurrentLives not found.");
        if (_pMax == null)
            Debug.LogWarning("[LivesHUD] LivesService.MaxLives not found.");

        Debug.Log("[LivesHUD] LivesService resolved.");
    }

    void TryBindEvents()
    {
        if (_livesService == null) { ResolveLivesService(); }
        if (_livesService == null) return;

        var t = _livesService.GetType();

        // We try common patterns:
        // public event Action<int,int> OnLivesChanged;  // (current, max)
        // public event Action<int>     LivesChanged;    // (current)
        // public event Action          OnReset;         // (reset all)
        string[] eventNames = { "OnLivesChanged", "LivesChanged", "OnReset", "OnLivesReset" };

        int boundCount = 0;
        foreach (var en in eventNames)
        {
            var ev = t.GetEvent(en, BindingFlags.Public | BindingFlags.Instance);
            if (ev == null) continue;

            try
            {
                var handler = CreateDelegateForEvent(ev, OnLivesEventProxy);
                if (handler != null)
                {
                    ev.RemoveEventHandler(_livesService, handler); // avoid dupes
                    ev.AddEventHandler(_livesService, handler);
                    boundCount++;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LivesHUD] Failed to bind '{en}': {e.Message}");
            }
        }

        _eventBound = boundCount > 0;
        Debug.Log($"[LivesHUD] Event binding result: bound={_eventBound} (handlers={boundCount}).");

        // Also do an immediate refresh
        UpdateHUD(force:true);
    }

    void TryUnbindEvents()
    {
        if (!_eventBound || _livesService == null) return;

        var t = _livesService.GetType();
        string[] eventNames = { "OnLivesChanged", "LivesChanged", "OnReset", "OnLivesReset" };

        foreach (var en in eventNames)
        {
            var ev = t.GetEvent(en, BindingFlags.Public | BindingFlags.Instance);
            if (ev == null) continue;
            try
            {
                var handler = CreateDelegateForEvent(ev, OnLivesEventProxy);
                if (handler != null)
                    ev.RemoveEventHandler(_livesService, handler);
            }
            catch { /* best-effort */ }
        }
        _eventBound = false;
    }

    // Converts our proxy method to the event’s signature (Action, Action<int>, Action<int,int>, etc.)
    Delegate CreateDelegateForEvent(EventInfo ev, Action proxy)
    {
        var evtHandlerType = ev.EventHandlerType;
        var invoke = evtHandlerType.GetMethod("Invoke");
        var parms = invoke.GetParameters();

        // We generate a wrapper that ignores args and calls UpdateHUD()
        if (parms.Length == 0)
            return Delegate.CreateDelegate(evtHandlerType, this, nameof(OnEvent0));
        if (parms.Length == 1)
            return Delegate.CreateDelegate(evtHandlerType, this, nameof(OnEvent1_IntOrObj));
        if (parms.Length == 2)
            return Delegate.CreateDelegate(evtHandlerType, this, nameof(OnEvent2_IntIntOrObjObj));

        // Fallback: bind to parameterless (will throw if incompatible)
        return Delegate.CreateDelegate(evtHandlerType, this, nameof(OnEvent0));
    }

    // These methods’ names MUST match CreateDelegateForEvent above
    void OnEvent0()                       { OnLivesEventProxy(); }
    void OnEvent1_IntOrObj(object _)      { OnLivesEventProxy(); }
    void OnEvent2_IntIntOrObjObj(object _, object __) { OnLivesEventProxy(); }

    void OnLivesEventProxy()
    {
        // Any event → refresh
        UpdateHUD(force:true);
    }

    // ─────────────────── UI update ───────────────────

    void UpdateHUD(bool force = false)
    {
        int current = ReadCurrent();
        int max     = ReadMax();

        if (!force && current == _lastCurrent && max == _lastMax)
            return;

        _lastCurrent = current;
        _lastMax     = max;

        string text = prefix + (showMax ? $"{current} / {max}" : $"{current}");

#if UNITY_2021_3_OR_NEWER
        if (_uiLabel != null) _uiLabel.text = text;
#endif
#if TMP_PRESENT
        if (tmpText != null) tmpText.text = text;
#endif
#if UNITY_UI || UNITY_UGUI
        if (uguiText != null) uguiText.text = text;
#endif

        if (_uiLabel == null
#if TMP_PRESENT
            && tmpText == null
#endif
#if UNITY_UI || UNITY_UGUI
            && uguiText == null
#endif
            )
        {
            // No UI bound? Still log it so you can see values updating.
            Debug.Log($"[LivesHUD] {text} (no label assigned)");
        }
    }

    int ReadCurrent()
    {
        if (_livesService == null || _pCurrent == null) return 0;
        try { return Convert.ToInt32(_pCurrent.GetValue(_livesService)); }
        catch { return 0; }
    }

    int ReadMax()
    {
        if (_livesService == null || _pMax == null) return 0;
        try { return Convert.ToInt32(_pMax.GetValue(_livesService)); }
        catch { return 0; }
    }
}
