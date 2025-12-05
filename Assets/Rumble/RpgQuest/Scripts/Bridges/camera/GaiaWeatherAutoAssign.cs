using UnityEngine;

[DisallowMultipleComponent]
public class GaiaWeatherAutoAssign : MonoBehaviour
{
    // Set these in inspector if you want to prefer a specific object

    public Transform preferredPlayer; // optional override

    // Name of the component field on Gaia; adjust if Gaia API differs
    public string gaiaComponentName = "Procedural Worlds Global Weather"; // display name only
    public string playerFieldName = "Player"; // the inspector field label - used only for debug

    void Awake()
    {
        if (TryAssign()) return;
        Debug.LogWarning("[GaiaAutoAssign] Could not automatically assign Player transform. Please assign manually.");
    }

    bool TryAssign()
    {
        // find the Gaia weather component on this GameObject
        var comp = GetComponent<MonoBehaviour>();
        if (comp == null)
        {
            Debug.LogWarning("[GaiaAutoAssign] No MonoBehaviour found on this GameObject to inspect.");
            return false;
        }

        // If user provided preferred transform, use it
        Transform candidate = preferredPlayer;

        // 1) Look for an object tagged "Player"
        if (candidate == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go) candidate = go.transform;
        }

        // 2) Fallback to Camera.main
        if (candidate == null && Camera.main != null)
            candidate = Camera.main.transform;

        // 3) Fallback: search for common names
        if (candidate == null)
        {
            var go = GameObject.Find("Player") ?? GameObject.Find("PlayerRoot") ?? GameObject.Find("PlayerCamera");
            if (go) candidate = go.transform;
        }

        if (candidate == null) return false;

        // Try to find the field/property using SerializedObject would be ideal,
        // but simplest reliable path is to set a public 'player' field if Gaia's component exposes it.
        // We'll try reflection to set a field/property named "player" or "Player".
        var t = comp.GetType();
        var f = t.GetField("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                ?? t.GetField("Player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Transform))
        {
            f.SetValue(comp, candidate);
            Debug.Log($"[GaiaAutoAssign] Assigned player Transform '{candidate.name}' to field '{f.Name}' on {t.Name}.");
            return true;
        }

        var p = t.GetProperty("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                ?? t.GetProperty("Player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(Transform) && p.CanWrite)
        {
            p.SetValue(comp, candidate);
            Debug.Log($"[GaiaAutoAssign] Assigned player Transform '{candidate.name}' to property '{p.Name}' on {t.Name}.");
            return true;
        }

        // If reflection fails, log what to do
        Debug.LogWarning($"[GaiaAutoAssign] Found candidate '{candidate.name}' but couldn't locate a Transform field/property named 'player' or 'Player' on component {t.Name}. Please assign manually.");
        return false;
    }
}

