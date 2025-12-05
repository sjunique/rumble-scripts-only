// using UnityEngine;
using UnityEngine;

 

using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest")]
    public Quest questToGive;                // assign ScriptableObject
    public DialogueSpeaker dialogueSpeaker;  // assign in Inspector

    [Header("Dialogue line indexes")]
    [Tooltip("Index for offering the quest")]
    public int offerLineIndex = 0;

    [Tooltip("Index for 'quest in progress' reminder")]
    public int inProgressLineIndex = 1;

    [Tooltip("Index for 'quest completed' line")]
    public int completeLineIndex = 2;

    [Header("Player filter")]
    [SerializeField] private string playerTag = "Player";

    bool _interactionLocked;   // optional guard to stop spam

    public void Interact()
    {
        if (_interactionLocked) return;
        _interactionLocked = true;
        Invoke(nameof(UnlockInteraction), 0.3f);

        if (questToGive == null || dialogueSpeaker == null)
        {
            Debug.LogError("[QuestGiver] Missing questToGive or dialogueSpeaker.", this);
            return;
        }

        var qm = QuestManager.Instance ?? FindObjectOfType<QuestManager>();
        if (qm == null)
        {
            Debug.LogError("[QuestGiver] QuestManager not found in scene.", this);
            return;
        }

        bool hasQuest = qm.HasQuest(questToGive);
        bool isCompleted = questToGive.isCompleted;

        // Safety: make sure we have at least one objective
        if (questToGive.objectives == null || questToGive.objectives.Length == 0)
        {
            Debug.LogWarning("[QuestGiver] Quest has no objectives.", this);
            dialogueSpeaker.PlayDialogue(offerLineIndex);  // just say something generic
            return;
        }

        var obj = questToGive.objectives[0];  // simple: first objective
        int required = obj.requiredAmount;
        string objectiveName = obj.objectiveName;

        if (!hasQuest && !isCompleted)
        {
            // Player is seeing this quest for the first time → offer + add
            dialogueSpeaker.PlayDialogue(offerLineIndex, required, objectiveName);
            qm.AddQuest(questToGive);
        }
        else if (hasQuest && !isCompleted)
        {
            // Quest is active but not done → reminder line
            dialogueSpeaker.PlayDialogue(inProgressLineIndex, required, objectiveName);
        }
        else if (isCompleted)
        {
            // Quest already completed → thank you line
            dialogueSpeaker.PlayDialogue(completeLineIndex, required, objectiveName);
            // (Optional: hand out rewards if not already done)
        }
    }

    void UnlockInteraction() => _interactionLocked = false;

    // Optional: if you want vSimpleTrigger to pass the collider:
    public void InteractFromTrigger(Collider other)
    {
        if (other == null) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        Interact();
    }
}
