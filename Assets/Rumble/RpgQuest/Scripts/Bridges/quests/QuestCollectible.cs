using UnityEngine;

 

namespace RpgQuest
{
    public class QuestCollectible : MonoBehaviour
    {
        [Tooltip("Must match an objectiveName in an active quest")]
        public string questObjectiveName;

        [Header("Behaviour")]
        [SerializeField] bool autoCollectOnTrigger = false;     // NEW
        [SerializeField] bool destroyOnCollect     = false;     // NEW

        private bool collected;

        [SerializeField] string playerTag = "Player";
        [SerializeField] LayerMask ignoreLayers;

        public void Collect()
        {
            if (collected) return;
            collected = true;

            var qm = QuestManager.Instance;
            if (qm == null)
            {
                Debug.LogWarning("[QuestCollectible] No QuestManager in scene.", this);
                if (destroyOnCollect) Destroy(gameObject);   // respect flag
                return;
            }

            var quests = qm.activeQuests.ToArray();
            Quest questToComplete = null;

            for (int q = 0; q < quests.Length; q++)
            {
                var quest = quests[q];
                if (quest == null || quest.objectives == null || quest.objectives.Length == 0) continue;

                bool allObjectivesComplete = true;

                var objectives = quest.objectives;
                for (int i = 0; i < objectives.Length; i++)
                {
                    var obj = objectives[i];
                    if (obj == null) continue;

                    if (obj.objectiveName == questObjectiveName)
                    {
                        if (obj.currentAmount < obj.requiredAmount)
                        {
                            obj.currentAmount++;
                            // Debug.Log($"[QuestCollectible] {questObjectiveName} -> {obj.currentAmount}/{obj.requiredAmount}");
                        }
                    }

                    if (obj.currentAmount < obj.requiredAmount)
                        allObjectivesComplete = false;
                }

                if (allObjectivesComplete)
                {
                    questToComplete = quest;
                    break;
                }
            }

            if (questToComplete != null)
            {
                Debug.Log($"[QuestCollectible] All objectives complete for quest: {questToComplete.questName}");
                qm.CompleteQuest(questToComplete);
            }
            else
            {
                Debug.LogWarning($"[QuestCollectible] No matching objective found or quest not yet complete for '{questObjectiveName}'.", this);
            }

            // 🔹 Only destroy if flag says so
            if (destroyOnCollect)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoCollectOnTrigger) return;   // 🔹 NEW – manual mode support

            if ((ignoreLayers.value & (1 << other.gameObject.layer)) != 0)
                return;

            var root = other.attachedRigidbody
                ? other.attachedRigidbody.transform.root.gameObject
                : other.transform.root.gameObject;

            if (!root.CompareTag(playerTag)) return;
            if (collected) return;

            Collect();

            var qrc = QuestRouteController.Instance ?? FindObjectOfType<QuestRouteController>();
            if (qrc != null)
            {
                qrc.NotifyCollectibleCollected();
                qrc.TryCompleteIfNoCollectiblesLeft();
            }
        }
    }
}






/*
namespace RpgQuest
{
    public class QuestCollectible : MonoBehaviour
    {
        [Tooltip("Must match an objectiveName in an active quest")]
        public string questObjectiveName;
        private bool collected;

        [SerializeField] string playerTag = "Player";
        [SerializeField] LayerMask ignoreLayers;


        public void Collect()
        {
            if (collected) return;
            collected = true;

            var qm = QuestManager.Instance ?? FindObjectOfType<QuestManager>();
            if (!qm)
            {
                Debug.LogError("[QuestCollectible] QuestManager not found.", this);
                collected = false;
                return;
            }

            Quest questToComplete = null;

            foreach (var quest in qm.activeQuests.ToArray())
            {
                if (quest == null || quest.objectives == null) continue;

                foreach (var obj in quest.objectives)
                {
                    if (obj == null) continue;
                    if (obj.objectiveName != questObjectiveName) continue;

                    // increment progress
                    if (obj.currentAmount < obj.requiredAmount)
                        obj.currentAmount++;

                    // check if every objective is now complete
                    bool done = true;
                    foreach (var check in quest.objectives)
                        if (check.currentAmount < check.requiredAmount)
                            done = false;

                    if (done)
                        questToComplete = quest;

                    break;
                }

                if (questToComplete != null) break;
            }

            if (questToComplete != null)
            {
                Debug.Log($"[QuestCollectible] Quest complete: {questToComplete.questName}");
                qm.CompleteQuest(questToComplete);
            }
            else
            {
                Debug.Log($"[QuestCollectible] Objective '{questObjectiveName}' progressed but quest not complete.");
            }

            // destroy collectible and notify route
          //  Destroy(gameObject);

            var qrc = QuestRouteController.Instance ?? FindObjectOfType<QuestRouteController>();
            if (qrc) { qrc.NotifyCollectibleCollected(); qrc.TryCompleteIfNoCollectiblesLeft(); }
        }


        private void OnTriggerEnter(Collider other)
        {
            if ((ignoreLayers.value & (1 << other.gameObject.layer)) != 0)
                return;

            var root = other.attachedRigidbody
                ? other.attachedRigidbody.transform.root.gameObject
                : other.transform.root.gameObject;

            if (!root.CompareTag(playerTag)) return;
            if (collected) return;

            Collect();

            var qrc = QuestRouteController.Instance ?? FindObjectOfType<QuestRouteController>();
            if (qrc != null)
            {
                qrc.NotifyCollectibleCollected();
                qrc.TryCompleteIfNoCollectiblesLeft();
            }
        }
    }
}
*/
