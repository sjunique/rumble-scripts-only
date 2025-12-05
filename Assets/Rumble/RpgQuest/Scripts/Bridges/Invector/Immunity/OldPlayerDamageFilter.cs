using UnityEngine;
using Invector.vCharacterController;
using Invector;


// Assets/Rumble/RpgQuest/Combat/PlayerDamageFilter.cs
 

//[RequireComponent(typeof(vHealthController))]
public class OldPlayerDamageFilter : MonoBehaviour
{
    ImmunityController imm;
    ShieldImmunityDriver shield;   // (below)
    vHealthController health;

    void Awake()
    {
        imm    = GetComponent<ImmunityController>();
        shield = GetComponent<ShieldImmunityDriver>();
        health = GetComponent<vHealthController>();
        health.onReceiveDamage.AddListener(OnReceiveDamage);   // fires before HP is applied
    }
    void OnDestroy(){ if (health) health.onReceiveDamage.RemoveListener(OnReceiveDamage); }

    void OnReceiveDamage(vDamage d)
    {
        // 1) Shield: block front-cone while raised & owned
        if (shield && shield.enabled && shield.ShouldBlock(d, transform))
        { d.damageValue = 0; shield.OnBlocked(d); return; }

        // 2) Perk-based immunities (Scuba, Jump i-frames, etc.)
        if (imm && imm.enabled && imm.IsImmuneTo(Categorize(d)))
        { d.damageValue = 0; return; }
    }

    // Map vDamage -> category (tweak if you later distinguish projectiles)
    DamageCategory Categorize(vDamage d)
    {
        if (d.sender)
        {
            int aiLayer = LayerMask.NameToLayer("AICreature");
            if (aiLayer >= 0 && d.sender.gameObject.layer == aiLayer)
                return DamageCategory.EnemyMelee;
        }
        if (d.sender == null) return DamageCategory.Fall;      // typical for fall/self
        return DamageCategory.Environmental;
    }
}

