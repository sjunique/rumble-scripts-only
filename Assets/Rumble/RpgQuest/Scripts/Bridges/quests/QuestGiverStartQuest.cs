using UnityEngine;
 
 


public class QuestGiverStartQuest : MonoBehaviour
{ 

    public Quest quest;
    [Tooltip("This should be the FIRST ROUTE TRIGGER GameObject (inactive at start).")]
    //public GameObject entryTriggerToEnable;

    [Header("Optional extra toggles")]
    public GameObject[] alsoEnableOnAccept;
    public GameObject[] disableOnAccept;

    [Header("Optional: disable interaction after accept")]
    public QuestGiverInteractionCoordinator interactionToDisableOnAccept;




     public void AcceptQuest()
    {
          var qm = QuestManager.Instance ?? FindObjectOfType<QuestManager>(true);
    if (qm && quest && !qm.HasQuest(quest) && !quest.isCompleted)
    {
        qm.AddQuest(quest);
        Debug.Log($"[QG] Added quest '{quest.questName}' to QuestManager.");
    }
        // if (!entryTriggerToEnable)
        // {
        //     Debug.LogError("[QG] entryTriggerToEnable is NULL. Drag your FirstRouteTrigger here.", this);
        //     return;
        // }

        // entryTriggerToEnable.SetActive(true);

        if (alsoEnableOnAccept != null)
            foreach (var go in alsoEnableOnAccept) if (go) go.SetActive(true);
        if (disableOnAccept != null)
            foreach (var go in disableOnAccept) if (go) go.SetActive(false);

        // NEW: tell the interaction coordinator this quest is accepted
        if (interactionToDisableOnAccept != null)
            interactionToDisableOnAccept.MarkAccepted();
 
    }
}


/*
 
    public class QuestGiverStartQuest : MonoBehaviour
    {
        public Quest quest;
        [Tooltip("This should be the FIRST ROUTE TRIGGER GameObject (inactive at start).")]
        public GameObject entryTriggerToEnable;

        [Header("Optional extra toggles")]
        public GameObject[] alsoEnableOnAccept;
        public GameObject[] disableOnAccept;

     public void AcceptQuest()
{
    var qm = QuestManager.Instance ?? FindObjectOfType<QuestManager>(true);
    if (qm && quest && !qm.HasQuest(quest) && !quest.isCompleted)
        qm.AddQuest(quest);

    if (!entryTriggerToEnable)
    {
        Debug.LogError("[QG] entryTriggerToEnable is NULL. Drag FirstRouteTrigger.", this);
        return;
    }

    // Ensure parents are active or warn
    bool parentActive = entryTriggerToEnable.transform.parent == null
                        || entryTriggerToEnable.transform.parent.gameObject.activeInHierarchy;
    if (!parentActive)
        Debug.LogWarning("[QG] Parent inactive; child won't be activeInHierarchy.", entryTriggerToEnable);

    entryTriggerToEnable.SetActive(true);
    Physics.SyncTransforms();

    // OPTIONAL: tiny nudge to force enter if already overlapping
    var link = PlayerCarLinker.Instance;
    var player = link ? link.player : null;
    if (player)
    {
        var rb = player.GetComponent<Rigidbody>();
        var p = player.transform.position;
        Vector3 epsilon = Vector3.one * 0.001f;
        if (rb) rb.position = p + epsilon;
        else    player.transform.position = p + epsilon;
    }

    if (alsoEnableOnAccept != null)
        foreach (var go in alsoEnableOnAccept) if (go) go.SetActive(true);
    if (disableOnAccept != null)
        foreach (var go in disableOnAccept) if (go) go.SetActive(false);
}

    }

*/
 
