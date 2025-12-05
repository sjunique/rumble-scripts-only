using UnityEngine;

namespace RpgQuest.Loot
{
    public enum LootRarity { Common, Uncommon, Rare, Epic, Legendary }

    [CreateAssetMenu(menuName = "RpgQuest/Loot/Item Def", fileName = "LootItemDef")]
    public class LootItemDef : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;                   // stable id (for saves/inventory)
        public string displayName;
        public Sprite icon;
        public LootRarity rarity = LootRarity.Common;

        [Header("Pickup / Visuals")]
        public GameObject worldPickupPrefab;    // if you want to scatter in world

        [Header("Stacking")]
        public int minQuantity = 1;
        public int maxQuantity = 1;

        [Header("Weight (probability)")]
        [Tooltip("Relative weight (e.g., Common 100, Rare 10). 0 disables the item.")]
        public int weight = 10;
    }
}

