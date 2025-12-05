// Assets/Rumble/RpgQuest/Combat/PlayerDamageFilter.cs
using UnityEngine;
using Invector.vCharacterController;
using Invector;
namespace Rumble.RpgQuest.Combat
{
    [DisallowMultipleComponent]
    public class PlayerDamageFilter : MonoBehaviour
    {
        public bool logDebug = false;
        [Tooltip("Use this if your Invector fires onReceiveDamage AFTER HP is reduced. It will refund the blocked damage.")]
        public bool useRefundFallback = true;

        ImmunityController imm;
        ShieldImmunityDriver shield;
        vHealthController health;

        void Awake()
        {
            imm = GetComponent<ImmunityController>();
            shield = GetComponent<ShieldImmunityDriver>();
            health = GetComponent<vHealthController>();

            if (!health)
            {
                Debug.LogError("PlayerDamageFilter: vHealthController not found on this GameObject. Put this on the PLAYER ROOT.");
                enabled = false; return;
            }

            health.onReceiveDamage.AddListener(OnReceiveDamage);
            if (logDebug) Debug.Log("[PlayerDamageFilter] Awake & listening");
        }

        void OnDestroy()
        {
            if (health) health.onReceiveDamage.RemoveListener(OnReceiveDamage);
        }

        void OnReceiveDamage(vDamage d)
        {
            if (d == null) return;

            // 1) Shield front-cone block
            if (shield && shield.enabled && shield.ShouldBlock(d, transform))
            {
                if (logDebug) Debug.Log($"[PlayerDamageFilter] BLOCK by Shield ({d.damageValue}) from {d.sender?.name}");
                TryBlock(d);
                shield.OnBlocked(d);
                return;
            }

            // 2) Selective immunity (scuba/jump/etc.)
            if (imm && imm.enabled && imm.IsImmuneTo(Categorize(d)))
            {
                if (logDebug) Debug.Log($"[PlayerDamageFilter] BLOCK by Immunity ({Categorize(d)}) amount {d.damageValue}");
                TryBlock(d);
                return;
            }

            if (logDebug) Debug.Log($"[PlayerDamageFilter] ALLOW {d.damageValue} from {d.sender?.name}");
        }

        void TryBlock(vDamage d)
        {
            // amount actually applied by Invector in some versions (float)
            float amount = d.damageValue;

            // 1) Try to cancel upstream (match type)
            d.damageValue = 0f;

            // 2) Refund if your onReceiveDamage fires AFTER HP was reduced
            if (useRefundFallback && amount > 0f && health)
            {
                // headroom until MaxHealth (float)
                float room = Mathf.Max(0f, health.MaxHealth - health.currentHealth);
                float refund = Mathf.Min(amount, room);

                int refundInt = Mathf.RoundToInt(refund);  // AddHealth(int) in your version
                if (refundInt > 0)
                {
                    health.AddHealth(refundInt);
                    if (logDebug)
                        Debug.Log($"[PlayerDamageFilter] REFUND {refundInt} -> HP {health.currentHealth}/{health.MaxHealth}");
                }
            }
        }


        DamageCategory Categorize(vDamage d)
        {
            if (d.sender)
            {
                int aiLayer = LayerMask.NameToLayer("AICreature");
                if (aiLayer >= 0 && d.sender.gameObject.layer == aiLayer)
                    return DamageCategory.EnemyMelee;
            }
            if (d.sender == null) return DamageCategory.Fall;
            return DamageCategory.Environmental;
        }
    }
}
