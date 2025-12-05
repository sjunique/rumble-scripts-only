using System.Collections.Generic;
using UnityEngine;

namespace RpgQuest.Loot
{
    [CreateAssetMenu(menuName = "RpgQuest/Loot/Loot Table", fileName = "LootTable")]
    public class LootTableSO : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public LootItemDef item;
            [Tooltip("Override item weight (<=0 means use item.weight)")]
            public int weightOverride = 0;
            [Tooltip("Override min..max quantity (0 = use item settings)")]
            public int minQtyOverride = 0;
            public int maxQtyOverride = 0;
        }

        [Header("Entries")]
        public List<Entry> entries = new();

        [Header("Roll Settings")]
        [Tooltip("How many picks are rolled from the table")]
        public int rolls = 3;

        [Tooltip("Allow the same item to be rolled multiple times")]
        public bool allowDuplicates = true;

        [Tooltip("When false, each item can only be won once per box")]
        public bool uniqueWithinRoll = false;

        public struct Rolled
        {
            public LootItemDef def;
            public int qty;
        }

        public List<Rolled> Roll(System.Random rng = null)
        {
            rng ??= new System.Random();
            var pool = BuildPool();
            var results = new List<Rolled>();
            var used = new HashSet<LootItemDef>();

            for (int i = 0; i < Mathf.Max(1, rolls); i++)
            {
                if (pool.totalWeight <= 0) break;

                var pick = Pick(pool, rng);
                if (pick == null) break;

                if (!allowDuplicates || uniqueWithinRoll)
                {
                    if (used.Contains(pick.def)) { i--; continue; }
                    used.Add(pick.def);
                }

                int minQ = pick.minQty > 0 ? pick.minQty : (pick.def ? pick.def.minQuantity : 1);
                int maxQ = pick.maxQty > 0 ? pick.maxQty : (pick.def ? pick.def.maxQuantity : 1);
                if (maxQ < minQ) maxQ = minQ;

                int qty = rng.Next(minQ, maxQ + 1);
                results.Add(new Rolled { def = pick.def, qty = qty });

                if (!allowDuplicates)
                {
                    // remove this def from the pool entirely
                    pool.Remove(pick.def);
                }
            }
            return results;
        }

        // ---- internal pool helpers ----
        class Pool
        {
            public struct Node { public LootItemDef def; public int weight; public int minQty; public int maxQty; }
            public List<Node> nodes = new();
            public int totalWeight;

            public void Add(LootItemDef def, int weight, int minQ, int maxQ)
            {
                if (!def || weight <= 0) return;
                nodes.Add(new Node { def = def, weight = weight, minQty = minQ, maxQty = maxQ });
                totalWeight += weight;
            }

            public void Remove(LootItemDef def)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    if (nodes[i].def == def)
                    {
                        totalWeight -= nodes[i].weight;
                        nodes.RemoveAt(i);
                        // do not break; remove all duplicates if any
                    }
                }
                if (totalWeight < 0) totalWeight = 0;
            }
        }

        Pool BuildPool()
        {
            var pool = new Pool();
            foreach (var e in entries)
            {
                if (!e.item) continue;
                int w = e.weightOverride > 0 ? e.weightOverride : e.item.weight;
                int minQ = e.minQtyOverride > 0 ? e.minQtyOverride : e.item.minQuantity;
                int maxQ = e.maxQtyOverride > 0 ? e.maxQtyOverride : e.item.maxQuantity;
                pool.Add(e.item, w, minQ, maxQ);
            }
            return pool;
        }

        class PickResult { public LootItemDef def; public int minQty; public int maxQty; }
        PickResult Pick(Pool pool, System.Random rng)
        {
            if (pool.totalWeight <= 0) return null;
            int r = rng.Next(1, pool.totalWeight + 1);
            int acc = 0;
            foreach (var n in pool.nodes)
            {
                acc += n.weight;
                if (r <= acc) return new PickResult { def = n.def, minQty = n.minQty, maxQty = n.maxQty };
            }
            return null;
        }
    }
}

