using UnityEngine;
 
public class SWSPathAutoHook : MonoBehaviour
{
    public WaypointPathVisualizer visualizer;
    void Reset() { visualizer = GetComponent<WaypointPathVisualizer>(); }
    void OnEnable()
    {
        if (!visualizer) return;
        if (!visualizer.pointsRoot) visualizer.pointsRoot = transform; // Path Manager object
        visualizer.Rebuild();
    }
}
