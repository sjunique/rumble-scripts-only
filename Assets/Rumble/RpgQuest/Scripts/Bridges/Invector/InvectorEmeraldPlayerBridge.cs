// On your Invector player (e.g., component named EmeraldAIPlayerDamage)
using UnityEngine;

public class EmeraldAIPlayerDamage : MonoBehaviour
{
    // Called by Emerald AI when the player is hit
    public void DamageInvectorPlayer(int DamageAmount, Transform Attacker)
    {
        var character = GetComponent<Invector.vCharacterController.vCharacter>();
        if (character == null) return;

        var meleeInput   = GetComponent<Invector.vCharacterController.vMeleeCombatInput>();
        var meleeManager = meleeInput != null ? meleeInput.meleeManager : null;

        var dmg = new Invector.vDamage(DamageAmount)
        {
            sender      = Attacker,
            hitPosition = Attacker ? Attacker.position : transform.position
        };

        // Respect blocking if you’re using melee
        if (meleeInput != null && meleeInput.isBlocking && meleeManager != null)
        {
            var reduction = meleeManager.GetDefenseRate();
            if (reduction > 0) dmg.ReduceDamage(reduction);
            meleeManager.OnDefense();
            dmg.reaction_id = meleeManager.GetDefenseRecoilID();
        }

        // Apply damage to Invector health
        character.TakeDamage(dmg);
    }
}
