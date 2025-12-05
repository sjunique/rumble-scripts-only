using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rumble/Upgrades/Upgrade Database", fileName = "UpgradeDatabase")]
public class UpgradeDatabase : ScriptableObject
{
    public List<UpgradeDef> upgrades = new();

    private Dictionary<UpgradeId, UpgradeDef> _lookup;
    public void BuildLookup()
    {
        _lookup = new Dictionary<UpgradeId, UpgradeDef>();
        foreach (var u in upgrades)
            if (u != null) _lookup[u.id] = u;
    }

    public UpgradeDef Get(UpgradeId id)
    {
        if (_lookup == null) BuildLookup();
        return _lookup != null && _lookup.TryGetValue(id, out var def) ? def : null;
    }
}
