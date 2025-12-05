using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeProgress : MonoBehaviour
{
    public static UpgradeProgress Instance { get; private set; }

    // keep in PlayerPrefs for now; swap to SaveSystem later
    const string KEY_PREFIX = "upg_";

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetLevel(UpgradeId id) => PlayerPrefs.GetInt(KEY_PREFIX + id, 0);

    public void SetLevel(UpgradeId id, int level)
    {
        PlayerPrefs.SetInt(KEY_PREFIX + id, Mathf.Max(0, level));
        PlayerPrefs.Save();
    }

    public bool IsMaxed(UpgradeDef def) => GetLevel(def.id) >= def.MaxLevel;

    public int NextCost(UpgradeDef def)
    {
        var current = GetLevel(def.id);
        return def.GetCostForLevel(current); // def returns int.MaxValue if past range
    }

    public bool TryBuy(UpgradeDef def)
    {
        if (IsMaxed(def)) return false;
        var cost = NextCost(def);
        if (!PlayerWallet.TrySpend(cost)) return false;
        SetLevel(def.id, GetLevel(def.id) + 1);
        return true;
    }
}
