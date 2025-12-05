 

using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class RuntimeBoxColliderVisualizer : MonoBehaviour
{
    private LineRenderer lr;
    private BoxCollider box;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        box = GetComponent<BoxCollider>();

        lr.loop = true;
        lr.useWorldSpace = true;
        lr.positionCount = 8;  // 8 corners
        lr.widthMultiplier = 0.02f;
    }

    void LateUpdate()
    {
        var b = box.bounds;

        Vector3 c = b.center;
        Vector3 e = b.extents;

        // 8 corners
        Vector3[] corners = new Vector3[8];
        corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
        corners[1] = c + new Vector3(e.x, -e.y, -e.z);
        corners[2] = c + new Vector3(e.x, -e.y, e.z);
        corners[3] = c + new Vector3(-e.x, -e.y, e.z);
        corners[4] = c + new Vector3(-e.x, e.y, -e.z);
        corners[5] = c + new Vector3(e.x, e.y, -e.z);
        corners[6] = c + new Vector3(e.x, e.y, e.z);
        corners[7] = c + new Vector3(-e.x, e.y, e.z);

        // You can choose a path; simplest is draw bottom + top + vertical edges.
        // For now, just feed the corners and let loop=true connect them in order:
        lr.SetPositions(corners);
    }
}
