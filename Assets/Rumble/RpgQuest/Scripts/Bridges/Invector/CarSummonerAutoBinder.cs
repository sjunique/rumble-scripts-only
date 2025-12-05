using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// Auto-wires WaterCarSummon at runtime, tolerant to spawn order & wrong inspector refs.
[DefaultExecutionOrder(200)]
public class CarSummonerAutoBinder : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] Component waterCarSummon; // drag the WaterCarSummon component if you like (Transform is okay; we'll fix it)

    [Header("Optional direct hints (override search)")]
    [SerializeField] Transform playerHint;
    [SerializeField] GameObject carRootHint;
    [SerializeField] Transform referenceFrameHint;

    [Header("Search hints")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] string carTag = "CarRoot";
    [SerializeField] string[] playerNameHints = { "Jammo_QuestPlayerONE", "Player", "vBasicController", "vThirdPersonController" };
    [SerializeField] string[] carNameHints = { "sci-fi_land_water_hover", "Hover", "Crawfish", "Bumble", "Raven" };

    [Header("Timing")]
    [SerializeField] float maxWaitSeconds = 6f;
    [SerializeField] float retryInterval = 0.15f;

    [Header("Logging")]
    [SerializeField] bool verbose = true;

    Coroutine bindRoutine;

    void OnEnable()
    {
        if (bindRoutine != null) StopCoroutine(bindRoutine);
        bindRoutine = StartCoroutine(BindWhenReady());
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // If a Transform was dragged by mistake, try to fix it in edit mode too
        var t = ResolveWaterCarSummonType();
        if (waterCarSummon && t != null && waterCarSummon.GetType() != t)
        {
            var real = GetComponent(t);
            if (real) waterCarSummon = (Component)real;
        }
    }
#endif

    IEnumerator BindWhenReady()
    {
        // --- Resolve the actual WaterCarSummon component on this GO ---
        var summonerType = ResolveWaterCarSummonType();
        if (summonerType == null)
        {
            Warn("Could not find type 'WaterCarSummon' in loaded assemblies.");
            yield break;
        }

        if (!waterCarSummon || waterCarSummon.GetType() != summonerType)
        {
            waterCarSummon = GetComponent(summonerType) as Component;
            if (!waterCarSummon)
            {
                Warn("No 'WaterCarSummon' component found on this GameObject.");
                yield break;
            }
        }

        // --- Wait for Player & Car (or use hints) ---
        float elapsed = 0f;
        Transform player = playerHint;
        GameObject carRoot = carRootHint;

        while (elapsed < maxWaitSeconds && (!player || !carRoot))
        {
            if (!player) player = FindPlayer();
            if (!carRoot) carRoot = FindCarRoot();
            if (player && carRoot) break;

            yield return new WaitForSeconds(retryInterval);
            elapsed += retryInterval;
        }

        // Final attempt
        if (!player) player = FindPlayer();
        if (!carRoot) carRoot = FindCarRoot();

        if (!player) { Warn("Player not found. Binding aborted."); yield break; }
        if (!carRoot) { Warn("Car Root not found. Binding aborted."); yield break; }

        var refFrame = referenceFrameHint ? referenceFrameHint : player;

        // 1) Try a friendly method first: SetRefs(player, carRoot, refFrame) with any compatible types
        if (!TryInvokeFlexibleSetRefs(waterCarSummon, player.gameObject, carRoot, refFrame))
        {
            // 2) Otherwise assign fields/properties by name, converting to the exact required type
            bool okP = TryAssignFlexible(waterCarSummon, new[] { "player", "Player", "playerController", "m_Player" }, player.gameObject);
            bool okC = TryAssignFlexible(waterCarSummon, new[] { "carRoot", "CarRoot", "hoverCarRoot", "m_CarRoot" }, carRoot);
            bool okR = TryAssignFlexible(waterCarSummon, new[] { "referenceFrame", "ReferenceFrame", "spawnReference", "m_ReferenceFrame" }, refFrame);

            if (!(okP && okC))
            {
                Warn("Reflection assign failed for one or more fields. Consider adding a public SetRefs(...) on WaterCarSummon.");
                yield break;
            }
        }

        if (verbose)
            Debug.Log($"[CarSummonerAutoBinder] ✅ Bound WaterCarSummon → Player='{player.name}', Car='{carRoot.name}', Ref='{refFrame.name}'", this);
    }

    // --------- Search helpers ---------
    Transform FindPlayer()
    {
        // tag
        var byTag = !string.IsNullOrEmpty(playerTag) ? GameObject.FindWithTag(playerTag) : null;
        if (byTag) return byTag.transform;

        // component types (resolved by name at runtime so no scripting define needed)
        var candidate = FindObjectWithAnyComponent(new[]
        {
            "Invector.vCharacterController.vThirdPersonController",
            "Invector.vCharacterController.vBasicController",
            "Invector.vCharacterController.vThirdPersonInput",
            "UnityEngine.CharacterController"
        });
        if (candidate) return candidate.transform;

        // names
        foreach (var n in playerNameHints)
        {
            var go = GameObject.Find(n);
            if (go) return go.transform;
        }
        // main camera root fallback
        return Camera.main ? Camera.main.transform.root : null;
    }

    GameObject FindCarRoot()
    {
        var byTag = !string.IsNullOrEmpty(carTag) ? GameObject.FindWithTag(carTag) : null;
        if (byTag) return byTag;

        var byType = FindObjectWithAnyComponent(new[]
        {
            "CarCameraRig", "RpgHoverController", "VehicleController", "HoverCarEnterExitHandler"
        });
        if (byType) return byType.gameObject;

        foreach (var n in carNameHints)
        {
            var found = FindStartsWith(n);
            if (found) return found;
        }
        return null;
    }

    GameObject FindStartsWith(string prefix)
    {
        var all = FindObjectsOfType<Transform>(true);
        foreach (var t in all)
            if (t.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        return null;
    }

    GameObject FindObjectWithAnyComponent(string[] qualifiedTypeNames)
    {
        foreach (var qn in qualifiedTypeNames)
        {
            var t = ResolveType(qn);
            if (t == null) continue;
            var comp = FindObjectOfType(t, true) as Component;
            if (comp) return comp.gameObject;
        }
        return null;
    }

    // --------- Flexible assignment / invocation ---------
    bool TryInvokeFlexibleSetRefs(Component target, GameObject playerGO, GameObject carGO, Transform refFrame)
    {
        // Find a method named SetRefs with 3 parameters and try to convert the three args
        var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var mi in methods)
        {
            if (mi.Name != "SetRefs") continue;
            var ps = mi.GetParameters();
            if (ps.Length != 3) continue;

            object a0 = ConvertToRequired(ps[0].ParameterType, playerGO, refFrame);
            object a1 = ConvertToRequired(ps[1].ParameterType, carGO, refFrame);
            object a2 = ConvertToRequired(ps[2].ParameterType, refFrame, refFrame);

            if (a0 == null || a1 == null || a2 == null) continue;

            try { mi.Invoke(target, new[] { a0, a1, a2 }); return true; }
            catch (Exception e) { Warn($"SetRefs invoke failed: {e.Message}"); return false; }
        }
        return false;
    }

    bool TryAssignFlexible(Component target, string[] names, UnityEngine.Object candidate)
    {
        foreach (var n in names)
        {
            // Field
            var f = target.GetType().GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var val = ConvertToRequired(f.FieldType, candidate, candidate);
                if (val != null) { f.SetValue(target, val); return true; }
            }
            // Property
            var p = target.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite)
            {
                var val = ConvertToRequired(p.PropertyType, candidate, candidate);
                if (val != null) { p.SetValue(target, val); return true; }
            }
        }
        return false;
    }

    object ConvertToRequired(Type required, UnityEngine.Object obj, UnityEngine.Object fallbackForGO)
    {
        if (obj == null || required == null) return null;

        // Exact match
        if (required.IsInstanceOfType(obj)) return obj;

        // GameObject/Transform conveniences
        var go = obj as GameObject ?? (obj is Component c ? c.gameObject : null);
        var tr = obj as Transform ?? (obj is Component c2 ? c2.transform : null);

        if (required == typeof(GameObject)) return go;
        if (required == typeof(Transform)) return tr;

        // If a specific Component is required (e.g., vThirdPersonController), fetch it from the GO
        if (typeof(Component).IsAssignableFrom(required) && go != null)
        {
            var comp = go.GetComponent(required);
            if (comp) return comp;
        }

        // Last resort: if required is a Component but we only have a Transform and it sits on the same GO
        if (typeof(Component).IsAssignableFrom(required) && fallbackForGO is Transform tf)
        {
            var comp = tf.GetComponent(required);
            if (comp) return comp;
        }

        return null;
    }

    // --------- Type resolution helpers ---------
    Type ResolveWaterCarSummonType() => ResolveType("WaterCarSummon");
    static Type ResolveType(string qualifiedOrSimpleName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(qualifiedOrSimpleName, false);
            if (t != null) return t;
            foreach (var tt in asm.GetTypes())
                if (tt.Name == qualifiedOrSimpleName) return tt;
        }
        return null;
    }

    void Warn(string m) { if (verbose) Debug.LogWarning($"[CarSummonerAutoBinder] {m}", this); }
}
