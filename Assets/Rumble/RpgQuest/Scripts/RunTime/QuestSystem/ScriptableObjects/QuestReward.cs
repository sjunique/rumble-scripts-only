using UnityEngine;
 

[CreateAssetMenu(menuName = "Quests/QuestReward")]
public class QuestReward : ScriptableObject
{
    public string rewardName;
    [TextArea] public string description;
    public Sprite icon;
    public RewardType rewardType;
    public int value;
    public string itemId;

    public enum RewardType
    {
        Immunity,
        Item,
        Teleport,
        Ability
    }
}
