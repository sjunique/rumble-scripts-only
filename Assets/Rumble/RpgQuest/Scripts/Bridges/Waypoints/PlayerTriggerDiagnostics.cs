using UnityEngine;

public class PlayerTriggerDiagnostics : MonoBehaviour
{
    void Start()
    {
        var col = GetComponent<Collider>();
        var rb  = GetComponent<Rigidbody>();

        Debug.Log($"[PTD] Player '{name}' tag={tag}, layer={gameObject.layer}");
        Debug.Log($"[PTD] Collider? {col!=null}, isTrigger={col && col.isTrigger}, enabled={col && col.enabled}");
        Debug.Log($"[PTD] Rigidbody? {rb!=null}, isKinematic={rb && rb.isKinematic}");

        if (!col) Debug.LogWarning("[PTD] Player missing Collider (waypoint triggers won’t fire).");
        else if (col.isTrigger) Debug.LogWarning("[PTD] Player collider isTrigger=TRUE (most waypoint triggers expect a solid player collider).");

        if (!rb) Debug.LogWarning("[PTD] Player missing Rigidbody (at least one of the overlapping colliders should have a Rigidbody).");
        // Kinematic is fine; Unity will still send trigger events as long as one has an RB.
    }
}
