
using System.Linq;  
using System;
 
using UnityEngine;
using System.Collections.Generic; //
[DefaultExecutionOrder(-5000)] // very early
public class UpgradeStateManager : MonoBehaviour
{
// ==== Equip state ====
private readonly System.Collections.Generic.Dictionary<UpgradeId, bool> _equipped = new();

// fired when the player equips/unequips an owned upgrade
public event System.Action<UpgradeId, bool> OnUpgradeEquippedChanged;

 





    
    [Header("Config")]
    [SerializeField] private UpgradeDatabase database;

    [Header("State")]
    [SerializeField] private int points = 0;

    // Levels per upgrade (0-based level index: 0 means level 0 / not purchased yet)
    private readonly Dictionary<UpgradeId, int> _levels = new();

    public static UpgradeStateManager Instance { get; private set; }

    // ===== Events (clear signatures) =====
    public event Action<int, int> OnPointsChanged;                                   // (oldPoints, newPoints)
    public event Action<UpgradeId, int, int> OnUpgradeLevelChanged;                   // (id, oldLevel, newLevel)
    public event Action<UpgradeId, int, int> OnUpgradePurchased;                      // (id, newLevel, pointsRemaining)
    public event Action OnStateLoaded;
    public event Action OnStateReset;

    // Back-compat: allow bootstrap to override the known upgrades list. 

    private List<UpgradeId> _knownOverride;


    // Back-compat: allow bootstrap to override the known upgrades list.


    public IEnumerable<UpgradeId> knownUpgrades
    {
        get
        {
            if (_knownOverride != null) return _knownOverride;

            if (database != null)
                return database.upgrades.Where(d => d != null).Select(d => d.id).ToList();

            // Fallback: all enum values
            return System.Enum.GetValues(typeof(UpgradeId)).Cast<UpgradeId>().ToList();
        }
        set
        {
            _knownOverride = (value != null) ? new List<UpgradeId>(value) : null;
        }
    }



    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (database != null) database.BuildLookup();
        InitLevelsIfNeeded();
        EnsureEquipDict();
        Load();
    }

private void EnsureEquipDict()
{
    foreach (UpgradeId id in System.Enum.GetValues(typeof(UpgradeId)))
        if (!_equipped.ContainsKey(id)) _equipped[id] = false;
}

public bool IsEquipped(UpgradeId id) => _equipped.TryGetValue(id, out var on) && on;

public bool CanEquip(UpgradeId id) => GetLevel(id) >= 1;

public void SetEquipped(UpgradeId id, bool equipped)
{
    if (!CanEquip(id)) equipped = false; // can’t equip if you don't own it
    if (_equipped.TryGetValue(id, out var cur) && cur == equipped) return;

    _equipped[id] = equipped;
    OnUpgradeEquippedChanged?.Invoke(id, equipped);
    Save(); // optional: persist equip; add below if you want
}



    public UpgradeDef GetDef(UpgradeId id)
    {
        return database != null ? database.Get(id) : null;
    }

    public void ForceStateLoaded()
    {
        OnStateLoaded?.Invoke();
    }

    // Legacy "has X" (level >= min). Defaults to level >= 1.
    public bool Has(UpgradeId id, int minLevel = 1) => GetLevel(id) >= minLevel;

    // Legacy "grant" used to give a free level (no cost). Keeps old semantics.
    public bool GrantFromQuest(UpgradeId id, int levels = 1)
    {


        var max = GetMaxLevel(id);
        if (max <= 0) return false;
        int oldLvl = GetLevel(id);
        int newLvl = Mathf.Clamp(oldLvl + levels, 0, max);
        if (newLvl == oldLvl) return false;

        _levels[id] = newLvl;

        // Fire events to keep UI/audio in sync
        OnUpgradeLevelChanged?.Invoke(id, oldLvl, newLvl);
        OnUpgradePurchased?.Invoke(id, newLvl, points); // okay to keep as "purchase-style" notify
        Debug.Log($"[UpgradeState] {id} level changed {oldLvl} -> {newLvl} (points={points})");

        Save();
        return true;
    }



    private void InitLevelsIfNeeded()
    {
        foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
            if (!_levels.ContainsKey(id)) _levels[id] = 0;
    }

    // ------- Public API -------
    public int Points => points;

    public int GetLevel(UpgradeId id) => _levels.TryGetValue(id, out var lvl) ? lvl : 0;

    public int GetMaxLevel(UpgradeId id) => database.Get(id)?.MaxLevel ?? 0;

    public bool IsMaxed(UpgradeId id) => GetLevel(id) >= GetMaxLevel(id);

    public int GetNextCost(UpgradeId id)
    {
        var def = database.Get(id);
        if (def == null) return int.MaxValue;
        int nextLevel = GetLevel(id); // next purchase increases this to +1
        return def.GetCostForLevel(nextLevel);
    }

    public bool CanAffordNext(UpgradeId id)
    {
        if (IsMaxed(id)) return false;
        return points >= GetNextCost(id);
    }

    public void AddPoints(int delta)
    {
        if (delta == 0) return;
        int old = points;
        points = Mathf.Max(0, points + delta);
        OnPointsChanged?.Invoke(old, points);

        Save();
    }

    public bool TryPurchase(UpgradeId id)
    {


        if (IsMaxed(id)) return false;


        int nextCost = GetNextCost(id);
        if (points < nextCost) return false;

        int oldLvl = GetLevel(id);
        int newLvl = oldLvl + 1;

        points -= nextCost;
        _levels[id] = newLvl;

        OnPointsChanged?.Invoke(points + nextCost, points);
        OnUpgradeLevelChanged?.Invoke(id, oldLvl, newLvl);
        OnUpgradePurchased?.Invoke(id, newLvl, points);
        Debug.Log($"[UpgradeState] {id} level changed {oldLvl} -> {newLvl} (points={points})");

        Save();
        return true;
    }

    // ------- Persistence -------
    const string KEY_POINTS = "UP_points";
    string KeyLevel(UpgradeId id) => $"UP_level_{id}";
string KeyEquip(UpgradeId id) => $"UP_equip_{id}";

public void Save()
{
    PlayerPrefs.SetInt(KEY_POINTS, points);
    foreach (var kv in _levels) PlayerPrefs.SetInt(KeyLevel(kv.Key), kv.Value);
    // save equip
    foreach (UpgradeId id in System.Enum.GetValues(typeof(UpgradeId)))
        PlayerPrefs.SetInt(KeyEquip(id), IsEquipped(id) ? 1 : 0);
    PlayerPrefs.Save();
}

public void Load()
{
    InitLevelsIfNeeded();
    EnsureEquipDict();
    points = PlayerPrefs.GetInt(KEY_POINTS, points);
    foreach (UpgradeId id in System.Enum.GetValues(typeof(UpgradeId)))
    {
        _levels[id] = PlayerPrefs.GetInt(KeyLevel(id), _levels[id]);
        _equipped[id] = PlayerPrefs.GetInt(KeyEquip(id), _equipped[id] ? 1 : 0) == 1 && GetLevel(id) >= 1;
    }
    OnStateLoaded?.Invoke();
}


    public void ResetAll(bool alsoResetPoints = true)
    {
        if (alsoResetPoints)
        {
            int old = points;
            points = 0;
            OnPointsChanged?.Invoke(old, points);
        }
        foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
        {
            int old = _levels[id];
            _levels[id] = 0;
            if (old != 0) OnUpgradeLevelChanged?.Invoke(id, old, 0);
        }
        Save();
        OnStateReset?.Invoke();
    }
}




// using System;
// using System.Collections.Generic;
// using UnityEngine;

// public class UpgradeStateManager : MonoBehaviour
// {
//     public static UpgradeStateManager Instance { get; private set; }

//     [Header("Catalog (optional)")]
//     public UpgradeDef[] knownUpgrades;

//     [Header("Economy")]
//     [SerializeField] int _points;
//     public int Points => _points;

//     // id -> level (0 = not owned)
//     readonly Dictionary<UpgradeId, int> _levels = new Dictionary<UpgradeId, int>();

//     public event Action<UpgradeId, int> OnUpgradeLevelChanged;
//     public event Action<int> OnPointsChanged;

//     const string SaveKeyLevels = "UPGRADES_LEVELS_V2";
//     const string SaveKeyPoints = "UPGRADES_POINTS_V2";

//     void Awake()
//     {
//         if (Instance && Instance != this) { Destroy(gameObject); return; }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//         Load();
//     }

//     public int GetLevel(UpgradeId id) => _levels.TryGetValue(id, out var lv) ? lv : 0;
//     public bool Has(UpgradeId id) => GetLevel(id) > 0;

//     public UpgradeDef GetDef(UpgradeId id)
//     {
//         if (knownUpgrades != null)
//             foreach (var d in knownUpgrades) if (d && d.id == id) return d;
//         return null;
//     }

//     public int CostForNextLevel(UpgradeId id)
//     {
//         var def = GetDef(id);
//         return def ? def.CostForNextLevel(GetLevel(id)) : int.MaxValue;
//     }

//     public bool IsMaxed(UpgradeId id)
//     {
//         var def = GetDef(id);
//         return !def || GetLevel(id) >= def.maxLevel;
//     }

//     public void AddPoints(int amount)
//     {
//         if (amount == 0) return;
//         _points = Mathf.Max(0, _points + amount);
//         PlayerPrefs.SetInt(SaveKeyPoints, _points);
//         PlayerPrefs.Save();
//         OnPointsChanged?.Invoke(_points);
//         Debug.Log($"[Upgrades] Points => {_points} (Δ={amount})");
//     }

//     public bool GrantFromQuest(UpgradeId id)
//     {
//         var def = GetDef(id); if (!def) return false;
//         int cur = GetLevel(id);
//         int target = Mathf.Max(cur, def.freeOnQuestGrant ? 1 : cur);
//         return SetLevel(id, target);
//     }

//     public bool TryBuyNextLevel(UpgradeId id)
//     {
//         var def = GetDef(id); if (!def) return false;
//         int cur = GetLevel(id);
//         if (cur >= def.maxLevel) return false;

//         int cost = def.CostForNextLevel(cur);
//         if (_points < cost) return false;

//         _points -= Mathf.Max(0, cost);
//         PlayerPrefs.SetInt(SaveKeyPoints, _points);
//         PlayerPrefs.Save();
//         OnPointsChanged?.Invoke(_points);
//         return SetLevel(id, cur + 1);
//     }

//     bool SetLevel(UpgradeId id, int newLevel)
//     {
//         newLevel = Mathf.Max(0, newLevel);
//         int before = GetLevel(id);
//         if (newLevel == before) return false;

//         _levels[id] = newLevel;
//         SaveLevels();
//         OnUpgradeLevelChanged?.Invoke(id, newLevel);
//         Debug.Log($"[Upgrades] {id} -> level {newLevel}");
//         return true;
//     }

//     void SaveLevels()
//     {
//         var sb = new System.Text.StringBuilder();
//         foreach (var kv in _levels)
//         {
//             if (sb.Length > 0) sb.Append(';');
//             sb.Append(kv.Key).Append(':').Append(kv.Value);
//         }
//         PlayerPrefs.SetString(SaveKeyLevels, sb.ToString());
//         PlayerPrefs.Save();
//     }

//     void Load()
//     {
//         _levels.Clear();
//         _points = PlayerPrefs.GetInt(SaveKeyPoints, 0);

//         string raw = PlayerPrefs.GetString(SaveKeyLevels, "");
//         if (!string.IsNullOrEmpty(raw))
//         {
//             var entries = raw.Split(';');
//             foreach (var e in entries)
//             {
//                 var kv = e.Split(':');
//                 if (kv.Length != 2) continue;
//                 if (Enum.TryParse(kv[0], out UpgradeId id) && int.TryParse(kv[1], out int lv))
//                     _levels[id] = Mathf.Max(0, lv);
//             }
//         }
//         else
//         {
//             // Legacy import (owned list → level 1)
//             string legacy = PlayerPrefs.GetString("UPGRADES_OWNED_V1", "");
//             if (!string.IsNullOrEmpty(legacy))
//             {
//                 foreach (var p in legacy.Split(','))
//                     if (Enum.TryParse(p, out UpgradeId id)) _levels[id] = 1;
//                 SaveLevels();
//             }
//         }
//     }

//     public void WipeAll()
//     {
//         _levels.Clear();
//         _points = 0;
//         PlayerPrefs.DeleteKey(SaveKeyLevels);
//         PlayerPrefs.DeleteKey(SaveKeyPoints);
//         PlayerPrefs.Save();
//         OnPointsChanged?.Invoke(_points);
//         OnUpgradeLevelChanged?.Invoke(0, 0);
//     }
// }
