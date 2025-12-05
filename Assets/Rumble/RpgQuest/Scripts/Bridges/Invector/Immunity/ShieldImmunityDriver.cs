using UnityEngine;
using Invector.vCharacterController;
using Invector;
// Assets/Rumble/RpgQuest/Combat/ShieldImmunityDriver.cs
 





 
[DisallowMultipleComponent]
public class ShieldImmunityDriver : MonoBehaviour
{
    [Header("Ownership & state")]
    public bool shieldOwned;     // from shop
    public bool shieldRaised;    // from input/anim

    [Header("Blocking Rules")]
    [Range(10,180)] public float blockConeDegrees = 120f;  // ±60° by default
    public LayerMask blockLayers;               // include AICreature
    public bool blockProjectilesAlso = true;    // expand later if you tag projectiles

    [Header("Feedback (optional)")]
    public AudioSource sfx;
    public AudioClip   blockClip;
    public GameObject  blockVfxPrefab;

    public bool ShouldBlock(vDamage d, Transform player)
    {
        if (!shieldOwned || !shieldRaised || d.sender == null) return false;

        if (blockLayers.value != 0 && (blockLayers.value & (1 << d.sender.gameObject.layer)) == 0)
            return false;

        Vector3 toAttacker = d.sender.position - player.position; toAttacker.y = 0f;
        if (toAttacker.sqrMagnitude < 0.0001f) return true;
        float ang = Vector3.Angle(player.forward, toAttacker.normalized);
        return ang <= (blockConeDegrees * 0.5f);
    }

    public void OnBlocked(vDamage d)
    {
        if (sfx && blockClip) sfx.PlayOneShot(blockClip);
        if (blockVfxPrefab)
        {
            var p = d.sender ? d.sender.position : transform.position + transform.forward * 0.5f;
            Instantiate(blockVfxPrefab, p, Quaternion.identity);
        }
        // tiny i-frame to avoid multi-tick when a melee collider sits inside you
        var imm = GetComponent<ImmunityController>();
        if (imm) imm.Add(DamageCategory.EnemyMelee, "ShieldBlockIFrame", 0.15f);
    }

    // Convenience APIs
    public void SetOwned(bool v)  => shieldOwned  = v;
    public void SetRaised(bool v) => shieldRaised = v;
}

