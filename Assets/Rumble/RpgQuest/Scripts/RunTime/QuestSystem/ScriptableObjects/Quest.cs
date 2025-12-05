using UnityEngine;

using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "RPG/Quest")]
public class Quest : ScriptableObject
{
    public string questName;
    public QuestReward reward;
    public bool isCompleted = false;
    public QuestObjective[] objectives;
}
 
 


// [System.Serializable]
// public class QuestObjective
// {
//     public string objectiveName;
//     public int requiredAmount;
//     public int currentAmount;
// }


// [CreateAssetMenu(menuName = "Quest/New Quest")]
// public class Quest : ScriptableObject
// {
//     public string questName;
//     [TextArea] public string description;
//     public QuestObjective[] objectives;
//     public bool isCompleted = false;
// }
