using UnityEngine;
using Invector.vCharacterController;
using Invector;
[DisallowMultipleComponent]
public class EmeraldToInvectorDamageReceiver : MonoBehaviour
{
    private vHealthController _health;

    void Awake()
    {
        _health = GetComponent<vHealthController>();
        if (_health == null)
            Debug.LogError("EmeraldToInvectorDamageReceiver: vHealthController missing on the player.");
    }

    // Emerald calls this on Attack Begin (newer versions with attacker)
    public void DamageCharacterController(int damageAmount, Transform attacker)
        => Apply(damageAmount, attacker);

    // Some setups call without attacker
    public void DamageCharacterController(int damageAmount)
        => Apply(damageAmount, null);

    private void Apply(int amount, Transform attacker)
    {
        if (_health == null || amount <= 0) return;

        var dmg = new vDamage
        {
            damageValue = amount,
            sender      = attacker,                                       // Transform (not GameObject)
            hitPosition = attacker ? attacker.position : transform.position
        };

        _health.TakeDamage(dmg);
    }
}
