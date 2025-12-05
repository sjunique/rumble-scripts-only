using UnityEngine;

// StartPointMarker.cs
using UnityEngine;

public class StartPointMarker : MonoBehaviour
{
    [SerializeField] private float gizmoSize = 2f;
    [SerializeField] private Color gizmoColor = Color.cyan;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoSize);
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 1.2f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * gizmoSize * 1.5f);
    }
}
