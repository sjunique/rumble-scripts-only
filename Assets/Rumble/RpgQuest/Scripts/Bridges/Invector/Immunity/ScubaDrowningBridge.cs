// Assets/Rumble/RpgQuest/Bridges/Combat/ScubaDrowningBridge.cs
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;

[DisallowMultipleComponent]
public class ScubaDrowningBridge : MonoBehaviour
{

    [Header("Scuba detection")]
    public string scubaTokenName = "scuba";        // if your controller uses tokens by string
    public string scubaUpgradeName = "Scuba";      // if your upgrade system uses names
    public bool forceScubaActive = false;          // manual override for testing



    [Header("Links (auto if left empty)")]
    public MonoBehaviour swimming;           // Invector vSwimming (or similar)
    public MonoBehaviour healthController;   // Invector vHealthController (or similar)
    public ImmunityController immunity;      // your existing immunity hub

    [Header("Behavior")]
    [Tooltip("If true, keeps oxygen near max when scuba is active & underwater.")]
    public bool refillOxygenUnderwater = true;

    [Tooltip("If true, cancels any drowning damage that still arrives while scuba is active.")]
    public bool cancelDrowningDamage = true;

    [Tooltip("Enable debug logs.")]
    public bool debugLogs = true;

    // reflection caches (to support slightly different API names across versions)
    FieldInfo fi_isUnderWater;
    FieldInfo fi_currentOxygen;
    FieldInfo fi_maxOxygen;
    FieldInfo fi_isDrowning;
    MethodInfo mi_ResetDrownTimer;

    // vHealthController.OnReceiveDamage (UnityEvent<vDamage>)
    UnityEvent<object> onReceiveDamageEvent; // boxed because we won't depend on vDamage type
    MethodInfo mi_AddListener;               // UnityAction<T>
    MethodInfo mi_RemoveListener;

    void Awake()
    {
        if (!immunity) immunity = GetComponent<ImmunityController>();

        if (!swimming)
            swimming = (MonoBehaviour)GetComponent("vSwimming");

        if (!healthController)
            healthController = (MonoBehaviour)GetComponent("vHealthController");

        // Cache swimming fields by common names
        if (swimming)
        {
            var t = swimming.GetType();
            fi_isUnderWater = FindBool(t, "isUnderWater", "IsUnderWater", "underWater");
            fi_currentOxygen = FindFloat(t, "currentOxygen", "oxygen", "CurrentOxygen");
            fi_maxOxygen = FindFloat(t, "maxOxygen", "MaxOxygen");
            fi_isDrowning = FindBool(t, "isDrowning", "IsDrowning");
            mi_ResetDrownTimer = t.GetMethod("ResetDrownTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (debugLogs)
                Debug.Log($"[ScubaBridge] Swim fields: UW={fi_isUnderWater != null} curO2={fi_currentOxygen != null} maxO2={fi_maxOxygen != null} isDrown={fi_isDrowning != null} reset={mi_ResetDrownTimer != null}");
        }
        else if (debugLogs) Debug.LogWarning("[ScubaBridge] vSwimming not found on player.");

        // Hook damage to cancel drowning if needed
        if (cancelDrowningDamage && healthController)
        {
            TrySubscribeDamage(true);
        }
    }

    void OnDestroy()
    {
        if (cancelDrowningDamage && healthController)
            TrySubscribeDamage(false);
    }

    void Update()
    {
        if (!refillOxygenUnderwater || immunity == null || swimming == null) return;

     //   bool scubaOn = immunity.Has("scuba");           // your ImmunityController token
        bool scubaOn = IsScubaActive();

        bool uw = GetBool(swimming, fi_isUnderWater);

        if (scubaOn && uw)
        {
            float maxO2 = GetFloat(swimming, fi_maxOxygen);
            if (maxO2 > 0f)
            {
                // keep oxygen high (don’t hard-lock to max to avoid jitter in UI)
                float cur = Mathf.Max(GetFloat(swimming, fi_currentOxygen), maxO2 * 0.95f);
                SetFloat(swimming, fi_currentOxygen, cur);

                // ensure drowning flag/timer is cleared
                SetBool(swimming, fi_isDrowning, false);
                mi_ResetDrownTimer?.Invoke(swimming, null);

                if (debugLogs)
                    Debug.Log($"[ScubaBridge] Underwater with Scuba — oxygen kept at ~{cur:0.0}/{maxO2:0.0}");
            }
        }
    }

    // -------- Damage hook --------

    void TrySubscribeDamage(bool subscribe)
    {
        // Find UnityEvent on vHealthController
        var t = healthController.GetType();

        // Try common names: OnReceiveDamage, onReceiveDamage, onDamageReceive
        var fiEvt = FindUnityEvent(t, "OnReceiveDamage", "onReceiveDamage", "onDamageReceive");
        if (fiEvt == null)
        {
            if (debugLogs) Debug.LogWarning("[ScubaBridge] Could not locate vHealthController.OnReceiveDamage event.");
            return;
        }

        var evt = fiEvt.GetValue(healthController);
        if (evt == null) return;

        onReceiveDamageEvent = evt as UnityEvent<object>; // box ok; we won't call Invoke directly

        // UnityEvent<T>.AddListener(UnityAction<T>)
        mi_AddListener = evt.GetType().GetMethod("AddListener");
        mi_RemoveListener = evt.GetType().GetMethod("RemoveListener");

        // Build a UnityAction<T> delegate that points to our handler
        var genArgs = evt.GetType().BaseType.GetGenericArguments(); // T = vDamage
        var unityActionType = typeof(UnityAction<>).MakeGenericType(genArgs);
        var handlerMethod = GetType().GetMethod(nameof(OnReceiveDamageBoxed), BindingFlags.Instance | BindingFlags.NonPublic);
        var del = System.Delegate.CreateDelegate(unityActionType, this, handlerMethod);

        if (subscribe) mi_AddListener?.Invoke(evt, new object[] { del });
        else mi_RemoveListener?.Invoke(evt, new object[] { del });
    }

    // Signature must be void Handler(T) — we accept object and reflect inside
    void OnReceiveDamageBoxed(object vDamage)
    {
        if (!cancelDrowningDamage || immunity == null) return;

        // If we have Scuba and we’re underwater, block damage that looks like drowning
     //   bool scubaOn = immunity.Has("scuba");
bool scubaOn = IsScubaActive();


        bool uw = GetBool(swimming, fi_isUnderWater);

        if (scubaOn && uw)
        {
            // Try to identify drowning damage: many controllers use a sender/attacker name "Drowning" or null
            // We'll zero out the amount if we can find a 'damageValue' float field.
            var fVal = vDamage.GetType().GetField("damageValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fVal != null)
            {
                float before = (float)fVal.GetValue(vDamage);
                fVal.SetValue(vDamage, 0f);
                if (debugLogs) Debug.Log($"[ScubaBridge] Cancelled drowning damage ({before:#.##}) due to Scuba.");
            }
        }
    }

    // -------- small reflection helpers --------
    static FieldInfo FindBool(System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool)) return f;
        }
        return null;
    }
    static FieldInfo FindFloat(System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(float)) return f;
        }
        return null;
    }
    static FieldInfo FindUnityEvent(System.Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && typeof(UnityEventBase).IsAssignableFrom(f.FieldType)) return f;
        }
        return null;
    }
    static bool GetBool(object o, FieldInfo f) => (o != null && f != null) ? (bool)f.GetValue(o) : false;
    static float GetFloat(object o, FieldInfo f) => (o != null && f != null) ? (float)f.GetValue(o) : 0f;
    static void SetBool(object o, FieldInfo f, bool v) { if (o != null && f != null) f.SetValue(o, v); }
    static void SetFloat(object o, FieldInfo f, float v) { if (o != null && f != null) f.SetValue(o, v); }
    

bool IsScubaActive()
{
    if (forceScubaActive) return true;

    // 1) Try ImmunityController-like methods: Has(string), IsImmune(string), HasToken(string)
    if (immunity != null)
    {
        var it = immunity.GetType();
        var m =
            it.GetMethod("Has",        BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[] { typeof(string) }, null) ??
            it.GetMethod("IsImmune",   BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[] { typeof(string) }, null) ??
            it.GetMethod("HasToken",   BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
        if (m != null)
        {
            var ok = (bool)m.Invoke(immunity, new object[] { scubaTokenName });
            if (debugLogs) Debug.Log($"[ScubaBridge] ImmunityController.{m.Name}(\"{scubaTokenName}\") => {ok}");
            if (ok) return true;
        }

        // 2) Try bool property/field names
        string[] names = { "Scuba", "IsScuba", "HasScuba", "ScubaActive", "isScuba", "hasScuba" };
        foreach (var n in names)
        {
            var p = it.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool))
            {
                var v = (bool)p.GetValue(immunity);
                if (debugLogs) Debug.Log($"[ScubaBridge] ImmunityController.{n} (prop) => {v}");
                if (v) return true;
            }
            var f = it.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool))
            {
                var v = (bool)f.GetValue(immunity);
                if (debugLogs) Debug.Log($"[ScubaBridge] ImmunityController.{n} (field) => {v}");
                if (v) return true;
            }
        }
    }

    // 3) Try an Upgrade system by name (e.g., UpgradeStateManager)
    var upg = GetComponent("UpgradeStateManager") ?? (object)GetComponentInParent(typeof(Component).Assembly.GetType("UpgradeStateManager"));
    if (upg != null)
    {
        var ut = upg.GetType();
        // Common patterns: IsEquipped(string), IsEnabled(string), Has(string)
        var tryNames = new[] { "IsEquipped", "IsEnabled", "Has" };
        foreach (var mn in tryNames)
        {
            var m = ut.GetMethod(mn, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
            if (m != null)
            {
                var ok = (bool)m.Invoke(upg, new object[] { scubaUpgradeName });
                if (debugLogs) Debug.Log($"[ScubaBridge] UpgradeStateManager.{mn}(\"{scubaUpgradeName}\") => {ok}");
                if (ok) return true;
            }
        }
    }

    // Not found anywhere
    return false;
}




}
