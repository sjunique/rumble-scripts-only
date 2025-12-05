using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class JammoQA_DialogBridge : MonoBehaviour
{
    [SerializeField] string acceptButtonName = "accept";
    [SerializeField] string declineButtonName = "decline";

    public UnityEvent OnAcceptClicked;
    public UnityEvent OnDeclineClicked;

    UIDocument doc;
    VisualElement root;
    Button acceptBtn, declineBtn;

    void OnEnable()
    {
        doc  = GetComponent<UIDocument>();
        root = doc.rootVisualElement;

        root.RegisterCallback<AttachToPanelEvent>(OnAttached);
        root.RegisterCallback<DetachFromPanelEvent>(OnDetached);

        if (root.panel != null) OnAttached(null); // domain reload off
    }

    void OnDisable()
    {
        if (root != null)
        {
            root.UnregisterCallback<AttachToPanelEvent>(OnAttached);
            root.UnregisterCallback<DetachFromPanelEvent>(OnDetached);
        }
        Unbind();
    }

    void OnAttached(AttachToPanelEvent _)
    {
        Unbind();

        acceptBtn  = root.Q<Button>(acceptButtonName);
        declineBtn = root.Q<Button>(declineButtonName);

        if (acceptBtn != null)
        {
            acceptBtn.pickingMode = PickingMode.Position;
            acceptBtn.focusable = true;
            acceptBtn.clicked += HandleAccept;
            Debug.Log($"[DialogBridge] Bound ACCEPT '{acceptButtonName}'");
        }
        else Debug.LogWarning($"[DialogBridge] Button '{acceptButtonName}' not found.");

        if (declineBtn != null)
        {
            declineBtn.pickingMode = PickingMode.Position;
            declineBtn.focusable = true;
            declineBtn.clicked += HandleDecline;
            Debug.Log($"[DialogBridge] Bound DECLINE '{declineButtonName}'");
        }
        else Debug.LogWarning($"[DialogBridge] Button '{declineButtonName}' not found.");
    }

    void OnDetached(DetachFromPanelEvent _) => Unbind();

    void Unbind()
    {
        if (acceptBtn  != null) acceptBtn.clicked  -= HandleAccept;
        if (declineBtn != null) declineBtn.clicked -= HandleDecline;
        acceptBtn = declineBtn = null;
    }

    void HandleAccept()  { Debug.Log("[DialogBridge] Accept CLICK");  OnAcceptClicked?.Invoke(); }
    void HandleDecline() { Debug.Log("[DialogBridge] Decline CLICK"); OnDeclineClicked?.Invoke(); }

    // for keyboard path
    public void InvokeAccept()  => HandleAccept();
    public void InvokeDecline() => HandleDecline();
}
