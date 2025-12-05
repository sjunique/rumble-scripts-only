using UnityEngine;

public class ShieldDamageGate : MonoBehaviour
{
    [Tooltip("Assign the ShieldRepelField on the player's shield child.")]
    public ShieldRepelField shield;

    void Awake()
    {
        if (!shield) shield = GetComponentInChildren<ShieldRepelField>(true);
    }

    /// <summary>Return true if damage should go through.</summary>
    public bool CanTakeDamage(Vector3 hitPoint, GameObject source)
    {
        return !(shield && shield.shieldActive);
    }

    /// <summary>Called when damage was blocked (play ripple/sfx here).</summary>
    public void OnBlocked(Vector3 hitPoint, Vector3 hitNormal, GameObject source)
    {
        // optional: you can call a ripple method on your shield VFX here
        // e.g., shield.SpawnHit(hitPoint);
    }
}
