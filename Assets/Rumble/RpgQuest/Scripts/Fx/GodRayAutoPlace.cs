using UnityEngine;
using UnityEngine;

[ExecuteAlways]
public class GodRayAutoPlace : MonoBehaviour
{
    [Header("Height")]
    public float rayHeight = 18f;       // matches ParticleSystem.main.startSizeY
    public float groundClearance = 0.3f;
    public LayerMask groundMask = ~0;   // use your Terrain/Default layers

    [Header("Apply to ParticleSystem")]
    public ParticleSystem targetPS;     // assign your LOD0 PS (will mirror to LOD1 if sibling named similarly)

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }

    void Apply()
    {
        if (!targetPS) targetPS = GetComponentInChildren<ParticleSystem>();
        if (!targetPS) return;

        // 1) Set particle height to rayHeight
        var main = targetPS.main;
        main.startSize3D = true;
        var s = main.startSizeY;
        main.startSizeY = rayHeight;

        // Mirror to LOD1 if present
        var lod1 = FindSiblingPS("LOD1_Lite");
        if (lod1)
        {
            var m1 = lod1.main;
            m1.startSize3D = true;
            m1.startSizeY = Mathf.Max(12f, rayHeight * 0.8f);
        }

        // 2) Find ground height under this XZ and place center = ground + rayHeight/2 + clearance
        float groundY = SampleGroundHeight(transform.position);
        var pos = transform.position;
        pos.y = groundY + (rayHeight * 0.5f) + groundClearance;
        transform.position = pos;

        // Optional: keep the emitter box centered
        var shape = targetPS.shape;
        shape.enabled = true;
        shape.position = new Vector3(0f, 0f, 0f); // keep emitter centered in the volume
    }

    float SampleGroundHeight(Vector3 worldPos)
    {
        // Try Terrain first
        var t = Terrain.activeTerrain;
        if (t)
        {
            float baseY = t.transform.position.y;
            float h = t.SampleHeight(worldPos);
            return baseY + h;
        }
        // Fallback: raycast
        Ray ray = new Ray(new Vector3(worldPos.x, worldPos.y + 200f, worldPos.z), Vector3.down);
        if (Physics.Raycast(ray, out var hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return worldPos.y; // no ground found, leave as is
    }

    ParticleSystem FindSiblingPS(string nameContains)
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps.gameObject.name.Contains(nameContains)) return ps;
        return null;
    }
}
