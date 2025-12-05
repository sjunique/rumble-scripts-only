using UnityEngine;

public class QuestGiverInteractionCoordinator : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] DialogueSpeaker speaker;
    [SerializeField] int offerLineIndex = 0;      // line used BEFORE quest is accepted
    [SerializeField] int completedLineIndex = -1; // optional "quest already done" line
    [SerializeField] JammoQADialogController jammoDialog;
    [SerializeField] float subtitleDuration = 3f;
    [SerializeField] float afterDelay = 0.1f;
    [SerializeField] string title = "Quest Title";
    [TextArea] [SerializeField] string body = "Do you accept?";
    [SerializeField] string legend = "E: Accept • Q/Esc: Decline";
    [SerializeField] string playerTag = "Player";

    [Header("Quest Gating")]
    [SerializeField] Quest quest;
    [SerializeField] bool blockIfQuestActive = true;
    [SerializeField] bool blockIfCompleted = true;

    bool running;
    bool permanentlyDisabled;
    bool completedBarkPlayed;

    void OnTriggerEnter(Collider other)
    {
        if (!other || !other.CompareTag(playerTag)) return;
        if (running || permanentlyDisabled) return;

        // If we have a quest, gate by its state
        if (quest != null)
        {
            var qm = QuestManager.Instance ?? FindObjectOfType<QuestManager>(true);
            bool hasQuest   = qm && qm.HasQuest(quest);
            bool isFinished = quest.isCompleted;

            // Completed → maybe one bark, but no accept/decline dialog
            if (blockIfCompleted && isFinished)
            {
                if (!completedBarkPlayed && completedLineIndex >= 0 && speaker != null)
                {
                    completedBarkPlayed = true;
                    // IMPORTANT: this line must have NO {0}/{1} placeholders
                    speaker.PlayDialogue(completedLineIndex);
                }
                permanentlyDisabled = true;
                return;
            }

            // Already active (accepted, not finished)
            if (blockIfQuestActive && hasQuest)
                return;
        }

        StartCoroutine(Run());
    }

    System.Collections.IEnumerator Run()
    {
        running = true;

        // IMPORTANT: offerLineIndex line should NOT have {0}/{1} placeholders,
        // because we are not passing any args.
        speaker?.PlayDialogue(offerLineIndex);

        yield return new WaitForSeconds(subtitleDuration + afterDelay);

        if (jammoDialog)
            jammoDialog.Show(title, body, legend);

        running = false;
    }

    // Called by QuestGiverStartQuest after Accept
    public void MarkAccepted()
    {
        permanentlyDisabled = true;

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }
}
