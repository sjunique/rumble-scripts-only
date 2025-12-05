

// ==============================
// QuestgiverQuickAdd (Editor).cs
// One-click: Add QuestgiverTalkController to selected, wire Main Camera head & visor, ensure Animator is set.
// Menu: Tools > Questgiver > Add Talk Controller to Selected
// ==============================
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class QuestgiverQuickAdd
{
    [MenuItem("Tools/Questgiver/Add Talk Controller to Selected", priority = 1001)]
    public static void AddTalkToSelected()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Questgiver", "Select at least one questgiver GameObject with an Animator.", "OK");
            return;
        }
        int added = 0;
        foreach (var go in selection)
        {
            var anim = go.GetComponent<Animator>();
            if (!anim)
            {
                Debug.LogWarning($"[Questgiver] {go.name} has no Animator—skipping.");
                continue;
            }
            var talk = go.GetComponent<QuestgiverTalkController>();
            if (!talk) { talk = go.AddComponent<QuestgiverTalkController>(); added++; }

            // Player head = Main Camera if available
            if (!talk.playerHead && Camera.main) talk.playerHead = Camera.main.transform;
            else if (!talk.playerHead)
            {
                var anyCam = Object.FindFirstObjectByType<Camera>();
                if (anyCam) talk.playerHead = anyCam.transform;
            }

            // Find visor-like renderer by name
            if (!talk.visorRenderer)
            {
                var rends = go.GetComponentsInChildren<Renderer>(true);
                talk.visorRenderer = rends.FirstOrDefault(r => r && r.name.ToLower().Contains("visor"));
            }

            // Ensure an AudioSource exists for voice (optional)
            if (!talk.voice)
            {
                var voiceGo = go.transform.Find("Voice")?.gameObject ?? new GameObject("Voice");
                if (!voiceGo.transform.parent) voiceGo.transform.SetParent(go.transform, false);
                var src = voiceGo.GetComponent<AudioSource>() ?? voiceGo.AddComponent<AudioSource>();
                src.playOnAwake = false; src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Custom;
                talk.voice = src;
            }

            // Make sure Animator always animates (avoid culling look-at)
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        EditorUtility.DisplayDialog("Questgiver", added > 0 ? $"Added Talk Controller to {added} object(s)." : "Controllers already present on selection.", "OK");
    }
}
#endif



