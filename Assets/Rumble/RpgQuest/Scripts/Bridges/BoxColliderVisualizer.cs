using UnityEngine;
 

[RequireComponent(typeof(BoxCollider))]
public class BoxColliderVisualizer : MonoBehaviour
{
    public Color boundsColor = Color.green;
    public bool showInPlayMode = true;

    private BoxCollider boxCollider;

    void OnDrawGizmos()
    {
        if (!enabled) return;
        
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) return;

        // Draw wireframe cube matching the BoxCollider's bounds
        Gizmos.color = boundsColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
    }

    void OnDrawGizmosSelected()
    {
        // Draw a solid preview when selected
        Gizmos.color = new Color(boundsColor.r, boundsColor.g, boundsColor.b, 0.1f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(boxCollider.center, boxCollider.size);
    }
}
