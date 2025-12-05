using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WaterTriggerFitter : MonoBehaviour
{
    [Tooltip("Extra padding over each bank (meters).")]
    public float bankPadding = 0.5f;

    [Tooltip("Half above / half below water surface.")]
    public float triggerHalfHeight = 1.0f; // => size.y = 2

    [Tooltip("Offset above water surface to ensure crossing.")]
    public float centerYOffset = 0.5f;

    void Reset() { Fit(); }
    void OnValidate() { Fit(); }

    void Fit()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        gameObject.tag = "Water";
        // Layer "Triggers" recommended, but don't force: user project may differ.

        // Use the water mesh/renderer bounds
        var mr = GetComponent<MeshRenderer>();
        if (!mr)
        {
            mr = GetComponentInChildren<MeshRenderer>();
            if (!mr) { Debug.LogWarning("[WaterTriggerFitter] No MeshRenderer found."); return; }
        }

        var b = mr.bounds;

        // Convert world bounds to local size relative to this transform
        var localCenter = transform.InverseTransformPoint(b.center);
        var worldSize = b.size;
        var localSize = transform.InverseTransformVector(worldSize);

        // We only care about absolute size magnitudes
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        // Pad X/Z so it overlaps the banks slightly; clamp Y to a thin band
        var size = new Vector3(localSize.x + bankPadding * 2f, triggerHalfHeight * 2f, localSize.z + bankPadding * 2f);
        var center = localCenter;
        center.y += centerYOffset; // sit slightly above the surface

        col.center = transform.InverseTransformPoint(transform.TransformPoint(center));
        col.size = size;
    }
}
