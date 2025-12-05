using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UpgradePanelToggleUIToolkit : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] KeyCode openCloseKey = KeyCode.Tab;
    [SerializeField] KeyCode[] closeKeys = new[] { KeyCode.Escape, KeyCode.Backspace };

    [Header("Player (optional)")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] GameObject player; // auto-find by tag if null
    [SerializeField] bool disableInvectorWhileOpen = true;

    UIDocument _doc;
    VisualElement _root;
    bool _open;
    bool _attached;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _doc.rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        _doc.rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

        if (!player && !string.IsNullOrEmpty(playerTag))
            player = GameObject.FindGameObjectWithTag(playerTag);

        // start hidden, even if root isn't ready yet
        SetOpen(false, log:true);
    }

    void OnAttachedToPanel(AttachToPanelEvent _)
    {
        _root = _doc.rootVisualElement;
        _attached = true;
        Debug.Log("[UpgradeToggle] Panel attached. Ensuring hidden by default.");
        ApplyVisibility(_open); // will set to hidden because _open is false after Awake call
    }

    void OnDetachedFromPanel(DetachFromPanelEvent _)
    {
        _attached = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(openCloseKey))
        {
            SetOpen(!_open, log:true);
        }
        else if (_open)
        {
            for (int i = 0; i < closeKeys.Length; i++)
            {
                if (Input.GetKeyDown(closeKeys[i]))
                {
                    SetOpen(false, log:true);
                    break;
                }
            }
        }
    }

    public void SetOpen(bool open, bool log = false)
    {
        if (_open == open && _attached) return;
        _open = open;

        if (open)
        {
            EventSystemOrchestrator.I?.SuspendAllUGUI();
            if (disableInvectorWhileOpen && player)
            {
                var invectorInput = player.GetComponent("vThirdPersonInput") as Behaviour;
                if (invectorInput) invectorInput.enabled = false;
            }
            UnityEngine.Cursor.visible = true;
           UnityEngine. Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            EventSystemOrchestrator.I?.ResumeAllUGUI();
            if (disableInvectorWhileOpen && player)
            {
                var invectorInput = player.GetComponent("vThirdPersonInput") as Behaviour;
                if (invectorInput) invectorInput.enabled = true;
            }
            // up to you if you want to re-lock here
          UnityEngine.  Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }

        ApplyVisibility(open);

        if (log)
            Debug.Log($"[UpgradeToggle] SetOpen({open})  attached={_attached}  display={( _attached ? _root?.resolvedStyle.display.ToString() : "n/a")}");
    }

    void ApplyVisibility(bool open)
    {
        if (!_attached)
        {
            // root not ready yet; when it attaches, OnAttached will apply current state
            return;
        }

        if (_root == null) _root = _doc.rootVisualElement;

        _root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        // make sure it can receive clicks
        _root.pickingMode = PickingMode.Position;
    }

    void OnDisable()
    {
        // safety: never leave uGUI suspended
        if (_open) SetOpen(false, log:true);
    }
}
