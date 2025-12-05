using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PointsStrip_TK : MonoBehaviour
{
    [Header("Formatting")]
    [SerializeField] private string prefix = "Points: ";
    [SerializeField] private bool thousands = true;
    [SerializeField] private Sprite iconSprite;    // optional

    [Header("Names in UXML")]
    [SerializeField] private string wrapName = "points-wrap";
    [SerializeField] private string textName = "text";
    [SerializeField] private string iconName = "icon";

    UIDocument _doc;
    Label _label;
    VisualElement _wrap, _icon;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        var root = _doc.rootVisualElement;
        _wrap  = root.Q<VisualElement>(wrapName);
        _label = root.Q<Label>(textName);
        _icon  = root.Q<VisualElement>(iconName);

        if (_icon != null && iconSprite != null)
        {
            // assign UI Toolkit background image from Sprite
            var tex = iconSprite.texture;
            _icon.style.backgroundImage = new StyleBackground(tex);
        }

        // initial draw + subscribe
        RefreshNow();

        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnPointsChanged += HandlePointsChanged;
            UpgradeStateManager.Instance.OnStateLoaded   += RefreshNow;
        }
    }

    void OnDisable()
    {
        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnPointsChanged -= HandlePointsChanged;
            UpgradeStateManager.Instance.OnStateLoaded   -= RefreshNow;
        }
    }

    void HandlePointsChanged(int oldPts, int newPts)
    {
        SetText(newPts);
        Flash();
    }

    void RefreshNow()
    {
        if (UpgradeStateManager.Instance == null) return;
        SetText(UpgradeStateManager.Instance.Points);
    }

    void SetText(int value)
    {
        if (_label == null) return;
        string num = thousands ? value.ToString("N0") : value.ToString();
        _label.text = $"{prefix}{num}";
    }

    // Light-weight visual feedback compatible with older UITK:
    void Flash()
    {
        if (_wrap == null) return;
        _wrap.EnableInClassList("pts-flash", true);
        // schedule removal after ~1 frame + transition time
        _wrap.schedule.Execute(() => _wrap.EnableInClassList("pts-flash", false))
             .StartingIn(180); // ms
    }
}

