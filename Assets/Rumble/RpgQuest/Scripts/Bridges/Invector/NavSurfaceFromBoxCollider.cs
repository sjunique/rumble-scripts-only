// NavSurfaceFromBoxCollider.cs
using UnityEngine;
using Unity.AI.Navigation;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(NavMeshSurface))]
public class NavSurfaceFromBoxCollider : MonoBehaviour
{
    public bool autoSync = true;
    BoxCollider box; NavMeshSurface surface;

    void OnEnable() { box = GetComponent<BoxCollider>(); surface = GetComponent<NavMeshSurface>(); Sync(); }
    void OnValidate(){ box = GetComponent<BoxCollider>(); surface = GetComponent<NavMeshSurface>(); Sync(); }

    void Sync()
    {
        if (!box || !surface) return;
        if (surface.collectObjects != CollectObjects.Volume) surface.collectObjects = CollectObjects.Volume;

        // BoxCollider size/center are local; NavMeshSurface expects local Center/Size too.
        surface.center = box.center;
        surface.size   = box.size;
    }

#if UNITY_EDITOR
    [ContextMenu("Bake This Volume")]
    public void BakeThis() { Sync(); surface.BuildNavMesh(); }

    [CustomEditor(typeof(NavSurfaceFromBoxCollider))]
    class E : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var t = (NavSurfaceFromBoxCollider)target;
            if (GUILayout.Button("Sync & Bake This Volume")) t.BakeThis();
        }
    }
#endif
}
