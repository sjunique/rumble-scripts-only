// Assets/Rumble/RpgQuest/Bridges/Combat/SpawnTeleportProtection.cs
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Invector.vCharacterController;
using Invector;
 
 

[DisallowMultipleComponent]
public class SpawnTeleportProtection : MonoBehaviour
{
    [Header("Protection Windows")]
    public float spawnProtectSeconds    = 1.0f;
    public float teleportProtectSeconds = 0.75f;
    public bool  zeroVelocityOnProtect  = true;

    vHealthController health;
    ImmunityController imm;
    Rigidbody rb;

    MethodInfo miResetHealth;    // () -> void
    MethodInfo miRevive;         // () -> void
    MethodInfo miAddHealthFloat; // (float) -> void
    MethodInfo miAddHealthInt;   // (int) -> void

    void Awake()
    {
        try
        {
            health = GetComponent<vHealthController>();
            imm    = GetComponent<ImmunityController>();
            rb     = GetComponent<Rigidbody>();

            if (!health) { Debug.LogError("[STP] vHealthController missing on Player."); enabled = false; return; }

            // resolve methods unambiguously
            var t = health.GetType();
            miResetHealth  = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .FirstOrDefault(m => m.Name == "ResetHealth" && m.GetParameters().Length == 0);
            miRevive       = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .FirstOrDefault(m => m.Name == "Revive" && m.GetParameters().Length == 0);
            miAddHealthFloat = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                               .FirstOrDefault(m => m.Name == "AddHealth" && m.GetParameters().Length == 1 &&
                                                    m.GetParameters()[0].ParameterType == typeof(float));
            miAddHealthInt   = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                               .FirstOrDefault(m => m.Name == "AddHealth" && m.GetParameters().Length == 1 &&
                                                    m.GetParameters()[0].ParameterType == typeof(int));

            if (zeroVelocityOnProtect && rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

            EnsureAliveAndToppedUp();
            ApplyProtect(spawnProtectSeconds, "SpawnProtect");

            Debug.Log("[STP] Ready (spawn protection active)");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[STP] Disabled due to exception: " + ex.Message);
            enabled = false;
        }
    }

    public void TeleportPulse()
    {
        if (!enabled) return;
        if (zeroVelocityOnProtect && rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        ApplyProtect(teleportProtectSeconds, "TeleportProtect");
    }

    void EnsureAliveAndToppedUp()
    {
        if (health.currentHealth <= 0.01f)
        {
            if (miResetHealth != null) { miResetHealth.Invoke(health, null); return; }
            if (miRevive      != null) { miRevive.Invoke(health, null);      return; }
            AddHealthToMax();
        }
        else
        {
            // comment this out if you want damage to carry across scenes
            AddHealthToMax();
        }
    }

    void AddHealthToMax()
    {
        float room = Mathf.Max(0f, health.MaxHealth - health.currentHealth);
        if (room <= 0.01f) return;

        if (miAddHealthFloat != null)      miAddHealthFloat.Invoke(health, new object[] { room });
        else if (miAddHealthInt != null)   miAddHealthInt.Invoke(health, new object[] { Mathf.RoundToInt(room) });
    }

    void ApplyProtect(float seconds, string token)
    {
        if (seconds <= 0f) return;

        if (imm && imm.enabled)
        {
            imm.Add(DamageCategory.All, token, seconds);
        }
        else
        {
            StartCoroutine(RefundWindow(seconds));
        }
    }

    System.Collections.IEnumerator RefundWindow(float s)
    {
        float until = Time.time + s;
        UnityAction<Invector.vDamage> cb = (dmg) =>
        {
            if (dmg == null || dmg.damageValue <= 0f || health == null) return;
            if (miAddHealthFloat != null)      miAddHealthFloat.Invoke(health, new object[] { dmg.damageValue });
            else if (miAddHealthInt != null)   miAddHealthInt.Invoke(health, new object[] { Mathf.RoundToInt(dmg.damageValue) });
        };

        health.onReceiveDamage.AddListener(cb);
        while (Time.time < until) yield return null;
        health.onReceiveDamage.RemoveListener(cb);
    }
}



// [DisallowMultipleComponent]
// public class SpawnTeleportProtection : MonoBehaviour
// {
//     [Header("Protection Windows")]
//     [Tooltip("Invulnerability seconds on scene start/spawn (begins in Awake).")]
//     public float spawnProtectSeconds = 1.0f;

//     [Tooltip("Invulnerability seconds whenever TeleportPulse() is called.")]
//     public float teleportProtectSeconds = 0.75f;

//     [Header("Fall helpers")]
//     [Tooltip("Zero Rigidbody velocity on Awake and TeleportPulse to avoid first-frame fall damage.")]
//     public bool zeroVelocityOnProtect = true;

//     public Transform player;
//     vHealthController health;
//     ImmunityController imm;
//     Rigidbody rb;

//     // Cached optional methods (vary by Invector version)
//     MethodInfo miResetHealth;    // () -> void
//     MethodInfo miRevive;         // () -> void
//     MethodInfo miAddHealthFloat; // (float) -> void
//     MethodInfo miAddHealthInt;   // (int) -> void

//     void Awake()
//     {
//         health = GetComponent<vHealthController>();
//         imm    = GetComponent<ImmunityController>();
//         rb     = GetComponent<Rigidbody>();

//         if (!health)
//         {
//             Debug.LogError("SpawnTeleportProtection: vHealthController missing on Player.");
//             enabled = false; return;
//         }

//         // --- Find methods unambiguously ---
//         var t = health.GetType();
//         miResetHealth  = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//                           .FirstOrDefault(m => m.Name == "ResetHealth" && m.GetParameters().Length == 0);
//         miRevive       = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//                           .FirstOrDefault(m => m.Name == "Revive" && m.GetParameters().Length == 0);
//         miAddHealthFloat = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
//                           .FirstOrDefault(m => m.Name == "AddHealth" &&
//                                                m.GetParameters().Length == 1 &&
//                                                m.GetParameters()[0].ParameterType == typeof(float));
//         miAddHealthInt   = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
//                           .FirstOrDefault(m => m.Name == "AddHealth" &&
//                                                m.GetParameters().Length == 1 &&
//                                                m.GetParameters()[0].ParameterType == typeof(int));

//         // Make sure we’re not spawning dead/with junk velocity
//         if (zeroVelocityOnProtect && rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
//         EnsureAliveAndToppedUp();

//         // Start protection immediately (Awake) so it covers the first FixedUpdate
//         ApplyProtect(spawnProtectSeconds, "SpawnProtect");
//     }

//     /// Call this right after SetPositionAndRotation / any snap / car mount moves
//     public void TeleportPulse()
//     {
//         if (zeroVelocityOnProtect && rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
//         ApplyProtect(teleportProtectSeconds, "TeleportProtect");
//     }

//     void EnsureAliveAndToppedUp()
//     {
//         // If we’re dead or at/below zero, prefer ResetHealth/Revive; otherwise top-up to Max
//         if (health.currentHealth <= 0.01f)
//         {
//             if (miResetHealth != null) { miResetHealth.Invoke(health, null); return; }
//             if (miRevive      != null) { miRevive.Invoke(health, null);      return; }
//             AddHealthToMax();
//         }
//         else
//         {
//             // Optional: carry over damage across scenes by commenting this out
//             AddHealthToMax();
//         }
//     }

//     void AddHealthToMax()
//     {
//         float room = Mathf.Max(0f, health.MaxHealth - health.currentHealth);
//         if (room <= 0.01f) return;

//         if (miAddHealthFloat != null)      miAddHealthFloat.Invoke(health, new object[] { room });
//         else if (miAddHealthInt != null)   miAddHealthInt.Invoke(health, new object[] { Mathf.RoundToInt(room) });
//         // else: no AddHealth found; nothing we can do
//     }

//     void ApplyProtect(float seconds, string token)
//     {
//         if (seconds <= 0f) return;

//         if (imm && imm.enabled)
//         {
//             // Our proper immunity pipeline
//             imm.Add(DamageCategory.All, token, seconds);
//         }
//         else
//         {
//             // Fallback: refund any damage taken during the window
//             StartCoroutine(RefundWindow(seconds));
//         }
//            var upright = player.GetComponent<UprightOnTeleport>();
//            if (upright) upright.OnTeleported();

//     }

//     System.Collections.IEnumerator RefundWindow(float s)
//     {
//         float until = Time.time + s;

//         UnityAction<Invector.vDamage> cb = (dmg) =>
//         {
//             if (dmg == null || dmg.damageValue <= 0f || health == null) return;

//             if (miAddHealthFloat != null)      miAddHealthFloat.Invoke(health, new object[] { dmg.damageValue });
//             else if (miAddHealthInt != null)   miAddHealthInt.Invoke(health, new object[] { Mathf.RoundToInt(dmg.damageValue) });
//         };

//         health.onReceiveDamage.AddListener(cb);
//         while (Time.time < until) yield return null;
//         health.onReceiveDamage.RemoveListener(cb);
//     }
// }
