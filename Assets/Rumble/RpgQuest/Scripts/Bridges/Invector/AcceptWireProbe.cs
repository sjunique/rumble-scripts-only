using UnityEngine;
 
public class AcceptWireProbe : MonoBehaviour
{
    public QuestGiverStartQuest giver;   // drag the NPC’s component here
    public GameObject firstRouteTrigger; // drag FirstRouteTrigger here

    public void Click()
    {
        string giverName = giver ? giver.name : "NULL";
        string trigPath  = firstRouteTrigger ? firstRouteTrigger.transform.GetHierarchyPath() : "NULL";
        Debug.Log($"[ACCEPT] Click -> giver={giverName}  trigger={trigPath}");

        if (giver)
        {
            giver.AcceptQuest(); // should enable FirstRouteTrigger
            Debug.Log("[ACCEPT] Called giver.AcceptQuest()");
        }
        else
        {
            Debug.LogError("[ACCEPT] giver is NULL (OpenPanel likely not passing it).");
        }

        if (firstRouteTrigger)
        {
            firstRouteTrigger.SetActive(true); // hard fallback so we can test
            Debug.Log($"[ACCEPT] Force-enabled trigger. activeSelf={firstRouteTrigger.activeSelf} activeInHierarchy={firstRouteTrigger.activeInHierarchy}");
        }
    }
}
