using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SubtitleUI_Isolated : MonoBehaviour
{
    public KeyCode testKey = KeyCode.F2;
    public string defaultSpeaker = "Quest Giver";
    [TextArea] public string defaultLine = "Bring me 5 shells.";
    public float defaultDuration = 3f;

    UIDocument doc; VisualElement wrap; Label spk, txt;
    bool attached; float timer;

    void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        doc.rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ => {
            var root = doc.rootVisualElement;
            wrap = root.Q<VisualElement>("subtitle-wrap");
            spk  = root.Q<Label>("subtitle-speaker");
            txt  = root.Q<Label>("subtitle-text");
            attached = true;
            Hide();
            Debug.Log("[SubtitleISO] attached");
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(testKey)) Show(defaultSpeaker, defaultLine, defaultDuration, immediate:true);
        if (timer > 0f) { timer -= Time.deltaTime; if (timer <= 0f) Hide(); }
    }

    public void Show(string speaker, string line, float duration, bool immediate=false)
    {
        if (!attached || wrap == null || txt == null) { Debug.LogWarning("[SubtitleISO] not ready"); return; }
        wrap.style.display = DisplayStyle.Flex;
        wrap.style.opacity = 1;
        spk.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
        spk.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;
        txt.text = line ?? "";
        timer = Mathf.Max(0.01f, duration);
    }

    public void Hide()
    {
        if (wrap == null) return;
        wrap.style.opacity = 0;
        wrap.style.display = DisplayStyle.None;
        if (spk != null) spk.text = "";
        if (txt != null) txt.text = "";
        timer = 0;
    }
}

