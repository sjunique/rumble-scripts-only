using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class QuestOverlayController : MonoBehaviour
{
    public enum ShowMode { SubtitleThenDialog, BothAtOnce, DialogOnly }

    [Header("Keys")]
    public KeyCode acceptKey = KeyCode.E;
    public KeyCode declineKey = KeyCode.Q;

    [Header("Events")]
    public UnityEvent OnAccept;
    public UnityEvent OnDecline;

    [Header("Timings")]
    public float subtitleDuration = 3f;      // how long to keep the subtitle
    public float delayBeforeDialog = 0.15f;  // small buffer after subtitle ends (SubtitleThenDialog)

    [Header("Names in UXML")]
    public string subtitleWrapName = "subtitle-wrap";
    public string subtitleSpeakerName = "subtitle-speaker";
    public string subtitleTextName = "subtitle-text";
    public string dialogOverlayName = "dialog-overlay"; // full-screen overlay wrapping the dialog
    public string dialogRootName = "dialog";            // the box with title/body/buttons
    public string dlgTitleName = "dlg-title";
    public string dlgBodyName = "dlg-body";
    public string dlgLegendName = "dlg-legend";
    public string dlgAcceptName = "accept";
    public string dlgDeclineName = "decline";

    UIDocument _doc;
    VisualElement _root;
    VisualElement _subWrap;
    Label _subSpeaker, _subText;

    VisualElement _dlgOverlay, _dlgRoot;
    Label _dlgTitle, _dlgBody, _dlgLegend;
    Button _btnAccept, _btnDecline;

    bool _attached, _dialogOpen;
    float _subTimer;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _doc.rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ => {
            _root = _doc.rootVisualElement;

            _subWrap    = _root.Q<VisualElement>(subtitleWrapName);
            _subSpeaker = _root.Q<Label>(subtitleSpeakerName);
            _subText    = _root.Q<Label>(subtitleTextName);

            _dlgOverlay = _root.Q<VisualElement>(dialogOverlayName);
            _dlgRoot    = _root.Q<VisualElement>(dialogRootName);
            _dlgTitle   = _root.Q<Label>(dlgTitleName);
            _dlgBody    = _root.Q<Label>(dlgBodyName);
            _dlgLegend  = _root.Q<Label>(dlgLegendName);
            _btnAccept  = _root.Q<Button>(dlgAcceptName);
            _btnDecline = _root.Q<Button>(dlgDeclineName);

           // if (_btnAccept)  _btnAccept.clicked  += HandleAccept;
           // if (_btnDecline) _btnDecline.clicked += HandleDecline;

 if (_btnAccept  != null) _btnAccept.clicked  += HandleAccept;
        if (_btnDecline != null) _btnDecline.clicked += HandleDecline;





         
HideSubtitle();
HideDialog();
EnsureSubtitleOnTop();    

if (_subWrap != null) _subWrap.pickingMode = PickingMode.Ignore;


            // ensure the subtitle doesn’t get masked by the dialog:
            //    if (_subWrap != null)  _subWrap.style.zIndex = 1001;   // on top
            //   if (_dlgOverlay != null) _dlgOverlay.style.zIndex = 1000;

            _attached = true;
            Debug.Log("[QuestOverlay] attached");
        });
    }
void EnsureSubtitleOnTop()
{
    if (_subWrap == null) return;

    // Preferred on newer UITK:
    try { _subWrap.BringToFront(); return; } catch { /* older versions may not have it */ }

    // Fallback for older UITK: remove & re-add as last sibling
    var parent = _subWrap.parent;
    if (parent != null)
    {
        parent.Remove(_subWrap);
        parent.Add(_subWrap); // last added = drawn on top
    }
}




    void Update()
    {
        if (!_attached) return;

        // dialog key path
        if (_dialogOpen)
        {
            if (Input.GetKeyDown(acceptKey)) HandleAccept();
            if (Input.GetKeyDown(declineKey) || Input.GetKeyDown(KeyCode.Escape)) HandleDecline();
        }

        // subtitle timer
        if (_subTimer > 0f)
        {
            _subTimer -= Time.deltaTime;
            if (_subTimer <= 0f) HideSubtitle();
        }
    }

    // ---------- Public API ----------

    // Use this for the “enter quest giver” sequence
    public void StartQuestOffer(string speaker, string subtitleLine,
                                string dlgTitle, string dlgBody, string dlgLegend,
                                ShowMode mode)
    {
        if (!_attached) return;

        switch (mode)
        {
            case ShowMode.SubtitleThenDialog:
                StopAllCoroutines();
                ShowSubtitle(speaker, subtitleLine, subtitleDuration);
                StartCoroutine(OpenDialogAfter(subtitleDuration + delayBeforeDialog, dlgTitle, dlgBody, dlgLegend));
                break;

            case ShowMode.BothAtOnce:
                StopAllCoroutines();
                // keep subtitle up while the dialog is open (no blink)
                ShowSubtitle(speaker, subtitleLine, subtitleDuration);
                ShowDialog(dlgTitle, dlgBody, dlgLegend);
                break;

            case ShowMode.DialogOnly:
                StopAllCoroutines();
                HideSubtitle();
                ShowDialog(dlgTitle, dlgBody, dlgLegend);
                break;
        }
    }

    public void ShowSubtitle(string speaker, string text, float duration)
    {
        if (_subWrap == null || _subText == null) return;

        _subWrap.style.display = DisplayStyle.Flex;
        _subWrap.style.visibility = Visibility.Visible;
        _subWrap.style.opacity = 1f;
        _subWrap.pickingMode = PickingMode.Ignore; // subtitles are visual only

        if (_subSpeaker != null)
        {
            _subSpeaker.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
            _subSpeaker.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        _subText.text = text ?? "";
        _subTimer = Mathf.Max(0.01f, duration);
    }

    public void HideSubtitle()
    {
        if (_subWrap == null) return;
        _subWrap.style.opacity = 0f;
        _subWrap.style.display = DisplayStyle.None;
        if (_subSpeaker != null) _subSpeaker.text = "";
        if (_subText != null) _subText.text = "";
        _subTimer = 0f;
    }

    public void ShowDialog(string title, string body, string legend)
    {
        if (_dlgOverlay == null) return;

        if (_dlgTitle != null && !string.IsNullOrEmpty(title))  _dlgTitle.text = title;
        if (_dlgBody  != null && !string.IsNullOrEmpty(body))   _dlgBody.text  = body;
        if (_dlgLegend!= null && !string.IsNullOrEmpty(legend)) _dlgLegend.text= legend;

        _dlgOverlay.style.display = DisplayStyle.Flex;
        _dlgOverlay.style.visibility = Visibility.Visible;
        _dialogOpen = true;
    }

    public void HideDialog()
    {
        if (_dlgOverlay == null) return;
        _dlgOverlay.style.display = DisplayStyle.None;
        _dialogOpen = false;
    }

    IEnumerator OpenDialogAfter(float t, string title, string body, string legend)
    {
        yield return new WaitForSeconds(t);
        ShowDialog(title, body, legend);
    }

    void HandleAccept()
    {
        OnAccept?.Invoke();
        HideDialog();
        // optional: keep/hide subtitle here — we leave it to the timer
    }

    void HandleDecline()
    {
        OnDecline?.Invoke();
        HideDialog();
    }
}
