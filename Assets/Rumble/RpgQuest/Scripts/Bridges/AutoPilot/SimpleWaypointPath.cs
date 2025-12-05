using System.Collections.Generic;
using UnityEngine;

public class SimpleWaypointPath : MonoBehaviour
{
    [Tooltip("If set, waypoints will be collected from this transform (and its children) instead of 'this'.")]
    public Transform collectionRoot;

    [Tooltip("Collect children recursively (subfolders). If off, only direct children are used.")]
    public bool recursive = false;

    [Tooltip("When true, will try to auto-detect a child named 'path' as the collection root.")]
    public bool tryFindChildNamedPath = true;

    // Read-only points view
    public IReadOnlyList<Transform> Points => _points;
    [SerializeField] private List<Transform> _points = new List<Transform>();

    void OnValidate() { Refresh(); }
    void Awake()      { Refresh(); }

    public void Refresh()
    {
        _points.Clear();

        Transform root = collectionRoot;
        if (!root && tryFindChildNamedPath)
        {
            var p = transform.Find("path");
            if (p) root = p;
        }
        if (!root) root = this.transform;

        if (recursive)
            CollectRecursive(root);
        else
            CollectDirectChildren(root);

        // strip nulls just in case
        _points.RemoveAll(t => t == null);
    }

    void CollectDirectChildren(Transform root)
    {
        foreach (Transform c in root) _points.Add(c);
    }

    void CollectRecursive(Transform root)
    {
        foreach (Transform c in root)
        {
            _points.Add(c);
            CollectRecursive(c);
        }
    }

    void OnDrawGizmos()
    {
        if (_points == null || _points.Count == 0) Refresh();
        if (_points.Count == 0) return;

        for (int i = 0; i < _points.Count; i++)
        {
            var t = _points[i];
            if (!t) continue;

            Gizmos.color = (i == 0 || i == _points.Count - 1) ? Color.green : Color.cyan;
            Gizmos.DrawSphere(t.position, 0.4f);

            if (i < _points.Count - 1 && _points[i + 1])
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(t.position, _points[i + 1].position);
            }
        }
    }
}
