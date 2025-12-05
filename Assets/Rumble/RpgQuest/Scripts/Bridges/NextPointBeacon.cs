using UnityEngine;

public class NextPointBeacon : MonoBehaviour
{
    public WaypointPathVisualizer visualizer;
    public Transform player;
    public float lookAhead = 3f;
    public float followLerp = 6f;
    public bool snapOnEnable = true;
    public bool snapOnRebuild = true;   // NEW
    public float verticalOffset = 0.6f;

    bool _snappedOnce;
    int _lastBuildVersion = -1;

    void OnEnable()
    {
        _snappedOnce = false;
        Subscribe(true);
        _lastBuildVersion = visualizer ? visualizer.PathBuildVersion : -1;
        TrySnapImmediate();
    }

    void OnDisable()
    {
        Subscribe(false);
    }

    void Subscribe(bool add)
    {
        if (!visualizer) return;
        if (add) visualizer.PathRebuilt += OnPathRebuilt;
        else     visualizer.PathRebuilt -= OnPathRebuilt;
    }

    void OnPathRebuilt()
    {
        _lastBuildVersion = visualizer ? visualizer.PathBuildVersion : -1;
        if (snapOnRebuild) TrySnapImmediate();
    }

    void Update()
    {
        if (!visualizer || visualizer.PathPoints == null || visualizer.PathPoints.Count < 2 || !player)
            return;

        // Detect changes even if we missed the event
        if (visualizer.PathBuildVersion != _lastBuildVersion)
        {
            _lastBuildVersion = visualizer.PathBuildVersion;
            if (snapOnRebuild) TrySnapImmediate();
        }

        int seg; float t;
        var closest = visualizer.ClosestPointOnPath(player.position, out seg, out t);
        var target  = visualizer.MarchForward(seg, t, lookAhead) + Vector3.up * verticalOffset;

        if (snapOnEnable && !_snappedOnce)
        {
            transform.position = target;   // first-frame snap
            _snappedOnce = true;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followLerp);
        transform.LookAt(player.position, Vector3.up);
    }

    void TrySnapImmediate()
    {
        if (!visualizer || visualizer.PathPoints == null || visualizer.PathPoints.Count < 2 || !player)
            return;

        int seg; float t;
        visualizer.ClosestPointOnPath(player.position, out seg, out t);
        var target = visualizer.MarchForward(seg, t, lookAhead) + Vector3.up * verticalOffset;
        transform.position = target;
        _snappedOnce = true;
    }
}
// This script is a beacon that follows the path visualizer, snapping to the next point ahead of the player.
// It can snap on enable or rebuild, and smoothly follows the path with a specified look-ahead distance.
// The beacon will always face the player, providing a clear visual cue of the next waypoint.
// It also handles path rebuilds to ensure it stays aligned with the current path configuration.
// The `snapOnRebuild` option allows it to automatically snap to the next point whenever the path is rebuilt, ensuring it remains relevant even after changes to the path structure.
// The `verticalOffset` allows the beacon to be positioned slightly above the path, making it more visible and avoiding potential collisions with the ground or other objects.