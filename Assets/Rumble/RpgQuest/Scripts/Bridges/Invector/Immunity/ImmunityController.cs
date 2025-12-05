 
// Assets/Rumble/RpgQuest/Combat/ImmunityController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum DamageCategory
{
    None            = 0,
    EnemyMelee      = 1 << 0,
    EnemyProjectile = 1 << 1,
    Fall            = 1 << 2,
    Environmental   = 1 << 3,
    Drowning        = 1 << 4,
    All             = ~0
}

[DisallowMultipleComponent]
public class ImmunityController : MonoBehaviour
{
    public DamageCategory ActiveMask { get; private set; } = DamageCategory.None;
    readonly Dictionary<string, DamageCategory> _sources = new();

    public bool IsImmuneTo(DamageCategory cat) => enabled && ((ActiveMask & cat) != 0);

    public void Add(DamageCategory mask, string source, float seconds = 0f)
    {
        if (string.IsNullOrEmpty(source)) source = Guid.NewGuid().ToString();
        _sources[source] = mask;
        Recompute();
        if (seconds > 0f) StartCoroutine(RemoveAfter(seconds, source));
    }

    public void Remove(string source) { if (_sources.Remove(source)) Recompute(); }
    public void ClearAll() { _sources.Clear(); Recompute(); }

    IEnumerator RemoveAfter(float s, string src) { yield return new WaitForSeconds(s); Remove(src); }

    void Recompute()
    {
        DamageCategory m = DamageCategory.None;
        foreach (var kv in _sources) m |= kv.Value;
        ActiveMask = m;
        // Debug.Log("[Immunity] " + ActiveMask);
    }
}

