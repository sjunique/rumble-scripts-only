using UnityEngine;
using System.Reflection;
using System.Collections;

public class EmeraldDetectionLayerFixer : MonoBehaviour
{
    [Header("Layer Names")]
    [Tooltip("The layer your Player uses. AIs will only detect this layer.")]
    public string playerLayerName = "Default";

    [Tooltip("The layer your Emerald AI enemies use. Will be removed from detection.")]
    public string aiLayerName = "AI";

    [Tooltip("Built-in Water layer will be stripped too.")]
    public string waterLayerName = "Water";

    [Header("Runtime Sync")]
    [Tooltip("Keep Emerald detection synced if the Player's layer changes at runtime.")]
    public bool followPlayerLayerAtRuntime = true;

    [Tooltip("How often to resync (seconds).")]
    public float refreshInterval = 1.0f;

    [Header("Extras")]
    [Tooltip("Also set PlayerTag to 'Player' if present.")]
    public bool enforcePlayerTag = true;

    [Tooltip("Set wide detection while testing.")]
    public bool relaxAngleAndRadius = true;

    public int testDetectionAngle = 360;
    public float testDetectionRadius = 35f;

    int _cachedPlayerLayer = -1;

    void Start()
    {
        // Cache current player layer (from the live player if present; else from name)
        _cachedPlayerLayer = TryGetLivePlayerLayer();
        if (_cachedPlayerLayer < 0)
            _cachedPlayerLayer = LayerMask.NameToLayer(playerLayerName);

        ApplyToAll();

        if (followPlayerLayerAtRuntime)
            StartCoroutine(RefreshLoop());
    }

    IEnumerator RefreshLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.05f, refreshInterval));
        while (true)
        {
            int live = TryGetLivePlayerLayer();
            if (live >= 0 && live != _cachedPlayerLayer)
            {
                _cachedPlayerLayer = live;
                ApplyToAll();
            }
            yield return wait;
        }
    }

    int TryGetLivePlayerLayer()
    {
        // If you have PlayerCarLinker, use it; otherwise find by tag.
        var linkType = System.Type.GetType("PlayerCarLinker");
        if (linkType != null)
        {
            var instProp = linkType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            var inst = instProp != null ? instProp.GetValue(null) : null;
            if (inst != null)
            {
                var playerField = linkType.GetField("player", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var player = playerField != null ? playerField.GetValue(inst) as Component : null;
                if (player && player.gameObject) return player.gameObject.layer;
            }
        }

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        return playerGo ? playerGo.layer : -1;
    }

    void ApplyToAll()
    {
        int playerLayer = _cachedPlayerLayer >= 0 ? _cachedPlayerLayer : LayerMask.NameToLayer(playerLayerName);
        if (playerLayer < 0)
        {
            Debug.LogWarning($"[EmeraldFix] Player layer '{playerLayerName}' not found; skipping.");
            return;
        }

        int aiLayer = LayerMask.NameToLayer(aiLayerName);
        int waterLayer = LayerMask.NameToLayer(waterLayerName);

        // Build the exact mask we want: ONLY the player's layer.
        int desiredMask = (1 << playerLayer);

        // Find all EmeraldAISystem components via type name (no hard dependency).
        var emeraldType = System.Type.GetType("EmeraldAI.EmeraldAISystem, Emerald AI");
        if (emeraldType == null)
        {
            // fallback search by name if assembly-qualified fails
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                emeraldType = asm.GetType("EmeraldAI.EmeraldAISystem");
                if (emeraldType != null) break;
            }
        }
        if (emeraldType == null)
        {
            Debug.LogWarning("[EmeraldFix] EmeraldAISystem type not found. Is Emerald AI imported?");
            return;
        }

        var all = FindObjectsOfType(emeraldType, true);
        int count = 0;

        foreach (var comp in all)
        {
            // Set DetectionLayers (LayerMask) or DetectionLayerMask (int) depending on version
            TrySetLayerMask(comp, "DetectionLayers", desiredMask);
            TrySetInt(comp, "DetectionLayerMask", desiredMask);

            if (relaxAngleAndRadius)
            {
                TrySetInt(comp, "DetectionAngle", testDetectionAngle);
                TrySetFloat(comp, "DetectionRadius", testDetectionRadius);
            }

            if (enforcePlayerTag)
                TrySetString(comp, "PlayerTag", "Player");

            count++;
        }

        Debug.Log($"[EmeraldFix] Applied detection mask (only '{LayerMask.LayerToName(playerLayer)}') to {count} Emerald AI(s). " +
                  $"Removed '{waterLayerName}' and '{aiLayerName}' implicitly.");
    }

    // ---------- Reflection helpers ----------

    static void TrySetLayerMask(object obj, string name, int maskValue)
    {
        var t = obj.GetType();
        // Property LayerMask
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType == typeof(LayerMask))
        {
            p.SetValue(obj, (LayerMask)maskValue);
            return;
        }
        // Field LayerMask
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(LayerMask))
        {
            f.SetValue(obj, (LayerMask)maskValue);
        }
    }

    static void TrySetInt(object obj, string name, int value)
    {
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType == typeof(int)) { p.SetValue(obj, value); return; }
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(int)) f.SetValue(obj, value);
    }

    static void TrySetFloat(object obj, string name, float value)
    {
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType == typeof(float)) { p.SetValue(obj, value); return; }
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(float)) f.SetValue(obj, value);
    }

    static void TrySetString(object obj, string name, string value)
    {
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType == typeof(string)) { p.SetValue(obj, value); return; }
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(string)) f.SetValue(obj, value);
    }
}
