using TMPro;
using UnityEngine;

public class QuestEntryUI : MonoBehaviour
{
    public TMP_Text questName;
    public TMP_Text objectives;

    public void Set(Quest quest)
    {
        questName.text = quest.questName;
        objectives.text = "";
        foreach (var obj in quest.objectives)
        {
            objectives.text += $"{obj.objectiveName}: {obj.currentAmount}/{obj.requiredAmount}\n";
        }
    }
}
