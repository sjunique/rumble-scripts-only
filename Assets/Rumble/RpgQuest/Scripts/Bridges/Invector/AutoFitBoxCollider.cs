using UnityEngine;
// AutoFitBoxCollider.cs
 

#if UNITY_EDITOR
[ExecuteAlways]
#endif
[RequireComponent(typeof(BoxCollider))]
public class AutoFitBoxCollider : MonoBehaviour
{
    public float padding = 0.02f;     // meters around the mesh
    public bool setTrigger = true;
    public bool onlyInEditor = true;  // avoids runtime fiddling

    void Reset()  { FitNow(); }
    void OnEnable(){ if (!Application.isPlaying || !onlyInEditor) FitNow(); }
    void OnValidate(){ if (!Application.isPlaying || !onlyInEditor) FitNow(); }

    [ContextMenu("Fit BoxCollider To Mesh")]
    public void FitNow()
    {
        var box = GetComponent<BoxCollider>();
        if (!box) return;

        // prefer local mesh bounds (exact), else fallback to renderer.worldBounds
        if (TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh)
        {
            var b = mf.sharedMesh.bounds;                 // local space
            box.center = b.center;
            box.size   = b.size + Vector3.one * (padding * 2f);
        }
        else if (TryGetComponent<SkinnedMeshRenderer>(out var sk))
        {
            var b = sk.localBounds;                       // local space
            box.center = b.center;
            box.size   = b.size + Vector3.one * (padding * 2f);
        }
        else if (TryGetComponent<Renderer>(out var r))
        {
            // fallback using world bounds → convert to local using lossyScale
            var lossy = transform.lossyScale;
            var sizeW = r.bounds.size;
            var sizeL = new Vector3(
                sizeW.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                sizeW.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                sizeW.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z))
            );
            box.center = transform.InverseTransformPoint(r.bounds.center);
            box.size   = sizeL + Vector3.one * (padding * 2f);
        }

        if (setTrigger) box.isTrigger = true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider>();
        if (!box) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        var m = Matrix4x4.TRS(transform.TransformPoint(box.center), transform.rotation, transform.lossyScale);
        using (new GizmoMatrixScope(m))
            Gizmos.DrawCube(Vector3.zero, box.size);
    }
    struct GizmoMatrixScope : System.IDisposable { Matrix4x4 prev; public GizmoMatrixScope(Matrix4x4 m){ prev=Gizmos.matrix; Gizmos.matrix=m; } public void Dispose(){ Gizmos.matrix=prev; } }
#endif
}
