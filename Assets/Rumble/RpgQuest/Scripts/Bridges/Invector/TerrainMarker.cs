using UnityEngine;

 
#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class TerrainMarker : MonoBehaviour
{
    public Color gizmoColor = Color.yellow;
    public float gizmoRadius = 1f;
    public bool drawLabel = true;
    public string markerLabel = "Marker";

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);

#if UNITY_EDITOR
        if (drawLabel)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (gizmoRadius + 0.2f),
                markerLabel,
                new GUIStyle { fontSize = 16, normal = new GUIStyleState { textColor = gizmoColor } }
            );
        }
#endif
    }
}
