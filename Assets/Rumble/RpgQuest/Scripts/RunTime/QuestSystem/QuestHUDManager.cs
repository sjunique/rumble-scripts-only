using UnityEngine;

public class QuestHUDManager : MonoBehaviour
{
    public GameObject questEntryPrefab;
    public Transform questListParent;

    void Update()
    {
        foreach (Transform child in questListParent)
            Destroy(child.gameObject);

        foreach (var quest in QuestManager.Instance.activeQuests)
        {
            var entry = Instantiate(questEntryPrefab, questListParent);
            entry.GetComponent<QuestEntryUI>().Set(quest);
        }
    }
}