 
using UnityEngine;

/// Keeps an object glued to terrain or any collider below it.
/// Works both in edit and play mode.
[ExecuteAlways]
public class GroundSync : MonoBehaviour
{
    [Tooltip("How far above to start the raycast.")]
    public float rayHeight = 5f;
    [Tooltip("Layers considered ground.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Align the up axis with the ground normal.")]
    public bool alignToNormal = true;
    [Tooltip("Y offset above the hit point (optional).")]
    public float surfaceOffset = 0.02f;
    [Tooltip("Run only once on start (good for spawned objects).")]
    public bool oneShot = true;

    void Start()
    {
        DoSnap();
        if (oneShot && Application.isPlaying)
            Destroy(this); // auto-remove after snap
    }

    void LateUpdate()
    {
        if (!oneShot || !Application.isPlaying)
            DoSnap();
    }

    void DoSnap()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundMask))
        {
            transform.position = hit.point + Vector3.up * surfaceOffset;
            if (alignToNormal)
                transform.up = hit.normal;
        }
    }
}
