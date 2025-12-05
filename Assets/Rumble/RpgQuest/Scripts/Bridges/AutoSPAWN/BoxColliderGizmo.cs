 

using UnityEngine;

[ExecuteAlways]   // So it works in Edit mode and Play mode
public class BoxColliderGizmo : MonoBehaviour
{
    void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
