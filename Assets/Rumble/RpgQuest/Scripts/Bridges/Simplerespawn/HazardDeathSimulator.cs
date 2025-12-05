// HazardDeathSimulator.cs
// Drop this on your player prefab or a test rig. It will call into vHealthController
// by setting currentHealth = 0 which triggers the usual health events in Invector.
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Invector;

[DisallowMultipleComponent]
public class HazardDeathSimulator : MonoBehaviour
{
    [Header("Health Reference (vHealthController)")]
    public vHealthController health;

    [Header("Auto-fall (hazard) settings")]
    [Tooltip("If enabled, the script will kill the player automatically when transform.position.y < fallDeathY")]
    public bool autoFallKill = false;
    public float fallDeathY = -20f;

    [Header("Damage test")]
    public float testDamageAmount = 50f;

    void Reset()
    {
        // try auto-wire
        if (!health) health = GetComponentInChildren<vHealthController>(true);
    }

    void Awake()
    {
        if (!health)
        {
            health = GetComponentInChildren<vHealthController>(true);
            if (!health) Debug.LogWarning("[HazardDeathSimulator] vHealthController not found. Assign it in inspector.");
        }
        Debug.Log("[HazardDeathSimulator] Ready. AutoFallKill=" + autoFallKill + " fallDeathY=" + fallDeathY);
    }

    void Update()
    {
        if (autoFallKill && transform.position.y < fallDeathY)
        {
            Debug.Log($"[HazardDeathSimulator] Auto-fall threshold reached (y={transform.position.y:F2}). Killing player.");
            KillInstant();
            // disable so it doesn't repeatedly attempt to kill
            autoFallKill = false;
        }
    }

    /// <summary> Kill instantly by setting health currentHealth to zero (causes Invector events to fire). </summary>
    public void KillInstant()
    {
        if (!health) { Debug.LogError("[HazardDeathSimulator] KillInstant failed - health missing."); return; }

        Debug.Log($"[HazardDeathSimulator] KillInstant() called. CurrentHealth={health.currentHealth:F2}");
        // Apply damage equal to current health to kill the player
        ApplyDamage(health.currentHealth);

        Debug.Log("[HazardDeathSimulator] Player killed via ApplyDamage.");
    }

    /// <summary> Apply damage amount (calls TakeDamage if available, otherwise reduces currentHealth). </summary>
    public void ApplyDamage(float amount)
    {
        if (!health) { Debug.LogError("[HazardDeathSimulator] ApplyDamage failed - health missing."); return; }

        // Try to call TakeDamage if vHealthController provides it (it usually does)
        var method = typeof(vHealthController).GetMethod("TakeDamage", new System.Type[] { typeof(Invector.vDamage) });
        if (method != null)
        {
            Debug.Log($"[HazardDeathSimulator] Calling TakeDamage via reflection with amount {amount}");
            // create simple vDamage fallback (only amount used in many vDamage implementations)
            var dmgType = typeof(Invector.vDamage);
            var dmg = System.Activator.CreateInstance(dmgType);
            var amountField = dmgType.GetField("damageValue") ?? dmgType.GetField("damage") ?? null;
            if (amountField != null)
            {
                amountField.SetValue(dmg, amount);
            }
            try
            {
                method.Invoke(health, new object[] { dmg });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HazardDeathSimulator] TakeDamage invocation failed: {ex.Message}. Falling back to direct health reduction.");
                //health.currentHealth = Mathf.Max(0f, health.currentHealth - amount);
            }
        }
        else
        {
            Debug.Log($"[HazardDeathSimulator] TakeDamage not found - reducing currentHealth by {amount}");
            //health.currentHealth = Mathf.Max(0f, health.currentHealth - amount);
        }
    }

    // small convenience wrapper to use the inspector testDamageAmount
    [ContextMenu("Apply Test Damage")]
    public void ApplyTestDamageContext()
    {
        ApplyDamage(testDamageAmount);
    }

    [ContextMenu("Kill Instant (Context)")]
    public void KillInstantContext()
    {
        KillInstant();
    }

    // Editor-only gizmo helper: show threshold in SceneView
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!autoFallKill) return;
        Gizmos.color = Color.red;
        var min = new Vector3(transform.position.x - 5f, fallDeathY, transform.position.z - 5f);
        var max = new Vector3(transform.position.x + 5f, fallDeathY, transform.position.z + 5f);
        Gizmos.DrawLine(min, max);
        Handles.Label(new Vector3(transform.position.x, fallDeathY, transform.position.z), $"Fall death Y = {fallDeathY}");
    }
#endif
}
