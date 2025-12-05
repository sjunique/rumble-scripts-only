using System.Collections;
using UnityEngine;

public class NewQuestGiverInteractionCoordinator : MonoBehaviour
{
    [Header("Who does what")]
    [SerializeField] private DialogueSpeaker speaker;         // your existing speaker (uses DialogueCollection)
    [SerializeField] private int dialogueLineIndex = 0;       // which line to play on contact
    [SerializeField] private JammoQuestDialogUI dialogUI;     // Toolkit dialog (with bridge for buttons)

    [Header("Subtitle timing")]
    [SerializeField] private float subtitleDuration = 3f;     // used if your DialogueLine has no duration
    [SerializeField] private float afterSubtitleDelay = 0.1f; // tiny buffer before opening dialog

    [Header("Dialog copy")]
    [SerializeField] private string dialogTitle  = "Quest Title";
    [TextArea] [SerializeField] private string dialogBody   = "Do you want to accept this quest?";
    [SerializeField] private string dialogLegend = "E: Accept • Q/Esc: Decline";

    [Header("Trigger filter")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool runOncePerEntry = true;

    bool _running;

    void OnTriggerEnter(Collider other)
    {
        if (!other || (playerTag != "" && !other.CompareTag(playerTag))) return;
        if (_running && runOncePerEntry) return;

        StartCoroutine(RunSequence(other.gameObject));
    }

    IEnumerator RunSequence(GameObject player)
    {
        _running = true;

        // 1) Play the quest-giver subtitle line
        if (speaker)
        {
            // You already have PlayDialogue(lineIndex, params)
            // It will call SubtitleUI.Show(...) (Toolkit) after our last changes
            speaker.PlayDialogue(dialogueLineIndex);
        }
        else
        {
            // Fallback: show a simple subtitle if no speaker set
            SubtitleUI.Show("", "…", subtitleDuration);
        }

        // 2) Wait for the subtitle to finish (use provided duration or your fallback)
        yield return new WaitForSeconds(subtitleDuration + afterSubtitleDelay);

        // 3) Open the Accept/Decline dialog
        if (dialogUI)
        {
         //   dialogUI.SetTexts(dialogTitle, dialogBody, dialogLegend);
            dialogUI.Show();
        }
        else
        {
            Debug.LogWarning("[QuestGiverInteraction] dialogUI not assigned.", this);
        }

        // allow re-trigger later if desired
        _running = false;
    }
}
