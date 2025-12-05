using UnityEngine;

 
// QuestGiverUiOpener.cs
using UnityEngine;

public class QuestGiverUiOpener : MonoBehaviour
{
    public QuestGiverStartQuest giver;  // drag the NPC’s component into this
    public DialogueOfferUI ui;

    public void OpenPanel()
    {
        if (!ui || !giver) { Debug.LogError("[Opener] Missing refs", this); return; }
        ui.Open(giver, "Quest", "Accept this quest?");
    }
}

 
