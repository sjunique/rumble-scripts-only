// Assets/Rumble/RpgQuest/Bridges/Combat/ScubaUnderwaterImmunity.cs
using UnityEngine;
using System.Reflection;

/// <summary>
/// Blocks underwater stamina drain and drowning while "Scuba" is equipped,
/// without modifying vendor code. Attach to the PLAYER root.
/// </summary>
[DisallowMultipleComponent]
public class ScubaUnderwaterImmunity : MonoBehaviour
{
    [Header("Wiring (auto if left blank)")]
    public MonoBehaviour vSwimming;        // your vSwimming MonoBehaviour
    public MonoBehaviour vThirdPersonCtrl; // vThirdPersonMotor / vThirdPersonController (holds stamina)

    [Header("Scuba control")]
    [Tooltip("Set true when the player equips Scuba; set false when unequipped.")]
    public bool scubaEquipped = false;

    [Tooltip("Quick test toggle to force scuba active without upgrades.")]
    public bool forceScuba = false;

    [Header("Debug")]
    public bool debugLogs = true;

    // --- vSwimming reflection ---
    FieldInfo fi_isUnderWater;
    FieldInfo fi_staminaDrain;        // "stamina" (float)
    FieldInfo fi_healthConsumption;   // "healthConsumption" (int or float)
    FieldInfo fi_isDrowning;          // optional "isDrowning" (bool)

    // --- controller (stamina) reflection ---
    FieldInfo fi_currentStamina;      // float
    FieldInfo fi_maxStamina;          // float

    // originals (to restore)
    float? originalStaminaDrain;
    float? originalHealthConsumption;

    void Awake()
    {
        if (!vSwimming)
            vSwimming = (MonoBehaviour)GetComponent("vSwimming");

        if (!vThirdPersonCtrl)
        {
            // Try common types by name (no hard deps)
            vThirdPersonCtrl = (MonoBehaviour)GetComponent("vThirdPersonMotor")
                             ?? (MonoBehaviour)GetComponent("vThirdPersonController")
                             ?? (MonoBehaviour)GetComponent("vThirdPersonAnimator");
        }

        if (!vSwimming)
        {
            if (debugLogs) Debug.LogWarning("[ScubaImm] vSwimming not found on player.");
            return;
        }

        // Cache vSwimming fields
        var st = vSwimming.GetType();
        fi_isUnderWater      = FindBool (st, "isUnderWater", "IsUnderWater", "underWater");
        fi_staminaDrain      = FindFloat(st, "stamina");                 // per-frame drain in water
        fi_healthConsumption = FindFloatOrInt(st, "healthConsumption");  // damage when out of stamina
        fi_isDrowning        = FindBool (st, "isDrowning", "IsDrowning");

        if (debugLogs)
            Debug.Log($"[ScubaImm] Swim fields: UW={fi_isUnderWater!=null} staminaDrain={fi_staminaDrain!=null} healthCons={fi_healthConsumption!=null} isDrown={fi_isDrowning!=null}");

        // Cache stamina fields on controller
        if (vThirdPersonCtrl)
        {
            var ct = vThirdPersonCtrl.GetType();
            fi_currentStamina = FindFloat(ct, "currentStamina", "CurrentStamina");
            fi_maxStamina     = FindFloat(ct, "maxStamina", "MaxStamina");
            if (debugLogs)
                Debug.Log($"[ScubaImm] Ctrl fields: curStam={fi_currentStamina!=null} maxStam={fi_maxStamina!=null}");
        }
        else if (debugLogs) Debug.LogWarning("[ScubaImm] vThirdPerson* stamina controller not found.");
    }

    void OnDisable()
    {
        // restore when component disables
        RestoreSwimTweaks();
    }

    void LateUpdate()
    {
        if (vSwimming == null || fi_isUnderWater == null) return;

        bool underwater = GetBool(vSwimming, fi_isUnderWater);
        bool scubaOn    = forceScuba || scubaEquipped;

        if (!underwater || !scubaOn)
        {
            RestoreSwimTweaks();
            return;
        }

        // Ensure we have cached originals before overwriting
        if (originalStaminaDrain == null && fi_staminaDrain != null)
            originalStaminaDrain = GetFloat(vSwimming, fi_staminaDrain);

        if (originalHealthConsumption == null && fi_healthConsumption != null)
            originalHealthConsumption = GetFloatFlexible(vSwimming, fi_healthConsumption);

        // 1) Top up stamina so the branch "currentStamina <= 0" never fires.
        if (vThirdPersonCtrl && fi_currentStamina != null && fi_maxStamina != null)
        {
            float maxS = GetFloat(vThirdPersonCtrl, fi_maxStamina);
            if (maxS > 0f)
            {
                SetFloat(vThirdPersonCtrl, fi_currentStamina, maxS);
            }
        }

        // 2) Neutralize the sources inside StaminaConsumption:
        // - no stamina drain
        if (fi_staminaDrain != null) SetFloat(vSwimming, fi_staminaDrain, 0f);
        // - no health damage when stamina at 0 (belt & suspenders)
        if (fi_healthConsumption != null) SetFloatFlexible(vSwimming, fi_healthConsumption, 0f);

        // 3) Clear any drowning flag if present
        if (fi_isDrowning != null) SetBool(vSwimming, fi_isDrowning, false);

        if (debugLogs)
            Debug.Log("[ScubaImm] Underwater + Scuba: stamina topped, drains zeroed, drowning cleared.");
    }

    void RestoreSwimTweaks()
    {
        if (vSwimming == null) return;

        if (originalStaminaDrain != null && fi_staminaDrain != null)
            SetFloat(vSwimming, fi_staminaDrain, originalStaminaDrain.Value);

        if (originalHealthConsumption != null && fi_healthConsumption != null)
            SetFloatFlexible(vSwimming, fi_healthConsumption, originalHealthConsumption.Value);

        originalStaminaDrain = null;
        originalHealthConsumption = null;
    }

    // ---- Public API to drive from your upgrade system ----
    public void SetScuba(bool equipped)
    {
        scubaEquipped = equipped;
        if (!equipped) RestoreSwimTweaks();
//        if (debugLogs) Debug.Log($"[ScubaImm] SetScuba({equipped})");
    }

    // -------- Reflection helpers --------
    static FieldInfo FindBool(System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool)) return f;
        }
        return null;
    }
    static FieldInfo FindFloat(System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(float)) return f;
        }
        return null;
    }
    static FieldInfo FindFloatOrInt(System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (f != null && (f.FieldType == typeof(float) || f.FieldType == typeof(int))) return f;
        }
        return null;
    }
    static bool  GetBool(object o, FieldInfo f)                => (o != null && f != null) ? (bool)f.GetValue(o)  : false;
    static float GetFloat(object o, FieldInfo f)               => (o != null && f != null) ? (float)f.GetValue(o) : 0f;
    static float GetFloatFlexible(object o, FieldInfo f)
    {
        if (o == null || f == null) return 0f;
        if (f.FieldType == typeof(float)) return (float)f.GetValue(o);
        if (f.FieldType == typeof(int))   return (int)f.GetValue(o);
        return 0f;
    }
    static void  SetBool (object o, FieldInfo f, bool v)       { if (o != null && f != null) f.SetValue(o, v); }
    static void  SetFloat(object o, FieldInfo f, float v)      { if (o != null && f != null) f.SetValue(o, v); }
    static void  SetFloatFlexible(object o, FieldInfo f, float v)
    {
        if (o == null || f == null) return;
        if (f.FieldType == typeof(float)) f.SetValue(o, v);
        else if (f.FieldType == typeof(int)) f.SetValue(o, Mathf.RoundToInt(v));
    }
}
