using UnityEngine;
using UnityEngine.UIElements;

public class DialogHotkeys : MonoBehaviour
{
    public UIDocument doc;                // leave null to auto-get on this GO
    public JammoQA_DialogBridge bridge;   // optional: if set, E/Q call bridge.InvokeAccept/InvokeDecline
    public KeyCode acceptKey = KeyCode.E;
    public KeyCode declineKey = KeyCode.Q;

    private VisualElement overlay, dialogBox;
    private Button acceptBtn, declineBtn;
    private Label titleLbl, bodyLbl, legendLbl;

    void Awake()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        if (doc == null) { Debug.LogError("[DialogHotkeys] No UIDocument found on GameObject."); enabled = false; return; }

        var root = doc.rootVisualElement;

        overlay   = root.Q<VisualElement>("dialog-overlay");
        dialogBox = root.Q<VisualElement>("dialog");
        acceptBtn = root.Q<Button>("accept");
        declineBtn= root.Q<Button>("decline");
        titleLbl  = root.Q<Label>("dlg-title");
        bodyLbl   = root.Q<Label>("dlg-body");
        legendLbl = root.Q<Label>("dlg-legend");

        Debug.Log($"[DialogHotkeys] found overlay={overlay!=null} dialog={dialogBox!=null} " +
                  $"acceptBtn={acceptBtn!=null} declineBtn={declineBtn!=null} " +
                  $"title={titleLbl!=null} body={bodyLbl!=null} legend={legendLbl!=null}");

        // Ensure dialog receives input on older UITK
        if (overlay != null) overlay.pickingMode = PickingMode.Position;
        if (dialogBox != null) dialogBox.pickingMode = PickingMode.Position;

        // If no external bridge, default buttons just hide the dialog
        if (acceptBtn != null)
            acceptBtn.clicked += () => { Debug.Log("[DialogHotkeys] Accept button clicked."); if (bridge != null) bridge.InvokeAccept(); Hide(); };

        if (declineBtn != null)
            declineBtn.clicked += () => { Debug.Log("[DialogHotkeys] Decline button clicked."); if (bridge != null) bridge.InvokeDecline(); Hide(); };
    }

    void Update()
    {
        if (overlay == null) return;

        // Only listen when visible and mounted
        bool visible = overlay.panel != null && overlay.resolvedStyle.display != DisplayStyle.None;
        if (!visible) return;

        if (Input.GetKeyDown(acceptKey)) {
            Debug.Log("[DialogHotkeys] Accept key pressed.");
            if (bridge != null) bridge.InvokeAccept();
            Hide();
        }

        if (Input.GetKeyDown(declineKey) || Input.GetKeyDown(KeyCode.Escape)) {
            Debug.Log("[DialogHotkeys] Decline key pressed.");
            if (bridge != null) bridge.InvokeDecline();
            Hide();
        }
    }

    public void Show(string title, string body, string legend = null)
    {
        if (overlay == null) { Debug.LogError("[DialogHotkeys] No 'dialog-overlay' element."); return; }

        if (titleLbl  != null) titleLbl.text  = title  ?? "";
        if (bodyLbl   != null) bodyLbl.text   = body   ?? "";
        if (legendLbl != null && !string.IsNullOrEmpty(legend)) legendLbl.text = legend;

        overlay.style.display = DisplayStyle.Flex;

        // Make sure the dialog is on top (older UITK without zIndex)
        try { overlay.BringToFront(); }
        catch {
            var parent = overlay.parent;
            if (parent != null) { parent.Remove(overlay); parent.Add(overlay); }
        }

        // Subtitle should not block clicks
        var sub = doc.rootVisualElement.Q<VisualElement>("subtitle-wrap");
        if (sub != null) sub.pickingMode = PickingMode.Ignore;

        Debug.Log("[DialogHotkeys] Show() -> overlay visible");
    }

    public void Hide()
    {
        if (overlay == null) return;
        overlay.style.display = DisplayStyle.None;
        Debug.Log("[DialogHotkeys] Hide() -> overlay hidden");
    }
}
