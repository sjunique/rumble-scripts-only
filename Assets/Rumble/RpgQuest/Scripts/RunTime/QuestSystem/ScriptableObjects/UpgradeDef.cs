using UnityEngine;
using UnityEngine;

[CreateAssetMenu(menuName = "Rumble/Upgrades/Upgrade Definition", fileName = "UpgradeDef")]
public class UpgradeDef : ScriptableObject
{
    [Header("Identity")]
    public UpgradeId id;

    [Header("UI")]
    public Sprite icon;

    [Header("Progression")]
    [Tooltip("Cost of each level in points. Length defines max level.")]
    public int[] costsPerLevel = new int[] { 50, 100, 150 };
   [TextArea(2, 4)]
    public string description;  // <— NEW field
    public int MaxLevel => costsPerLevel != null ? costsPerLevel.Length : 0;

    public int GetCostForLevel(int nextLevelIndex)
    {
        if (costsPerLevel == null || nextLevelIndex < 0 || nextLevelIndex >= costsPerLevel.Length)
            return int.MaxValue;
        return costsPerLevel[nextLevelIndex];
    }
}

// public enum UpgradeId { Shield, Scuba, Laser }

// [CreateAssetMenu(menuName = "Game/Upgrade Def")]
// public class UpgradeDef : ScriptableObject
// {
//     public UpgradeId id;
//     public string displayName;
//     [TextArea] public string description;
//     public Sprite icon;

//     [Header("Levels")]
//     [Min(1)] public int maxLevel = 1;

//     [Tooltip("Cost to buy each level (index 0 = cost to reach level 1). " +
//              "If shorter than maxLevel, missing entries are treated as 0.")]
//     public int[] levelCosts;

//     [Tooltip("If granted by a quest, should it bump to level 1 for free?")]
//     public bool freeOnQuestGrant = true;

//     public int CostForNextLevel(int currentLevel)
//     {
//         if (currentLevel >= maxLevel) return int.MaxValue;
//         int idx = Mathf.Clamp(currentLevel, 0, (levelCosts != null ? levelCosts.Length - 1 : 0));
//         return (levelCosts != null && idx < levelCosts.Length) ? levelCosts[idx] : 0;
//     }
// }
