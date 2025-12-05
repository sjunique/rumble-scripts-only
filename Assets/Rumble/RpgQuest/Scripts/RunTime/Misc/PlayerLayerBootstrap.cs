using System.Linq;
using UnityEngine;

/// Put this on the Player prefab root so every spawned clone fixes its layers.
public class PlayerLayerBootstrap : MonoBehaviour
{
    [Header("Layer names (must exist in Project Settings > Tags & Layers)")]
    public string playerLayer = "PlayerCharacter";
    public string helperLayer = "NonCombat";

    [Header("Optional: add Emerald Faction Extension at runtime")]
    public bool ensureFactionExtension = true;
    public string playerFactionName = "Player";

    [Header("Rescan options")]
    public bool rescanNextFrame = true;   // catches helpers enabled after Awake (IK/footstep)
    public float periodicRescanSeconds = 0f; // set >0 if weapons/helpers get added later

    void Awake()
    {
        ApplyOnce();
        if (rescanNextFrame) StartCoroutine(RescanNextFrame());
        if (periodicRescanSeconds > 0f) InvokeRepeating(nameof(ApplyOnce), periodicRescanSeconds, periodicRescanSeconds);
    }

    System.Collections.IEnumerator RescanNextFrame()
    {
        yield return null; // wait a frame so late-enabled helpers are present
        ApplyOnce();
    }

    void ApplyOnce()
    {
        // keep ROOT on PlayerCharacter
        SetLayer(gameObject, playerLayer);

        // move helper bits to NonCombat
        int moved = 0;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t == transform) continue;
            var go = t.gameObject;

            if (IsHelper(go))
            {
                SetLayer(go, helperLayer);
                moved++;
            }
        }

        // (optional) ensure Emerald Faction Extension exists & is set to Player
        if (ensureFactionExtension) TryAttachFactionExtension();

        // Debug.Log($"{name}: PlayerLayerBootstrap moved {moved} helper objects to {helperLayer}.");
    }

    bool IsHelper(GameObject go)
    {
        // name-based match (covers your left/rightFoot_trigger, hitBox, etc.)
        string n = go.name.ToLowerInvariant();
        if (n.Contains("trigger") || n.Contains("hitbox") || n.Contains("toe") || n.Contains("foot"))
            return true;

        // component-type match without hard assembly refs
        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (!c) continue;
            string cn = c.GetType().Name;
            if (cn == "vFootStepTrigger" || cn == "vHitBox" || cn == "vBodyPart" || cn == "vRagdoll")
                return true;
        }
        return false;
    }

    void SetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1) go.layer = layer;
    }

    void TryAttachFactionExtension()
    {
        // Try namespaced first
        var type = System.Type.GetType("EmeraldAI.FactionExtension, Assembly-CSharp", false);
        if (type == null) type = System.Type.GetType("FactionExtension, Assembly-CSharp", false);
        if (type == null) return;

        var existing = GetComponent(type);
        if (!existing) existing = gameObject.AddComponent(type);

        // Best-effort set the faction name if a field/property exists
        var f = type.GetField("Faction") ?? type.GetField("m_FactionName");
        if (f != null) { try { f.SetValue(existing, playerFactionName); } catch { } }

        var p = type.GetProperty("Faction");
        if (p != null && p.CanWrite) { try { p.SetValue(existing, playerFactionName); } catch { } }
    }
}
