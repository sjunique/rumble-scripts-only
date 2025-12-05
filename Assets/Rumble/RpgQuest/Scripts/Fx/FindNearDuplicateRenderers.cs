using UnityEngine;
using System.Collections.Generic;

public class FindNearDuplicateRenderers : MonoBehaviour
{
    public float posEpsilon = 0.02f;
    public float rotEpsilonDeg = 1f;
    public float scaleEpsilon = 0.01f;
    public string[] nameFilters = new[] { "Tree", "Grass", "Bush" };

    void Start()
    {
        var renderers = GameObject.FindObjectsOfType<Renderer>();
        var buckets = new Dictionary<Vector3, List<Transform>>();
        foreach (var r in renderers)
        {
            bool ok = false;
            foreach (var f in nameFilters) if (r.name.Contains(f)) { ok = true; break; }
            if (!ok) continue;

            var p = r.transform.position;
            var key = new Vector3(Mathf.Round(p.x/posEpsilon)*posEpsilon,
                                  Mathf.Round(p.y/posEpsilon)*posEpsilon,
                                  Mathf.Round(p.z/posEpsilon)*posEpsilon);
            if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<Transform>();
            list.Add(r.transform);
        }
        foreach (var kv in buckets)
        {
            var list = kv.Value;
            if (list.Count < 2) continue;
            for (int i = 0; i < list.Count; i++)
            for (int j = i+1; j < list.Count; j++)
            {
                var a = list[i]; var b = list[j];
                if (Vector3.Distance(a.position,b.position) <= posEpsilon &&
                    Quaternion.Angle(a.rotation,b.rotation) <= rotEpsilonDeg &&
                    Vector3.Distance(a.lossyScale,b.lossyScale) <= scaleEpsilon)
                {
                    Debug.LogWarning($"Near-duplicate: {a.name} & {b.name} at {a.position}");
                }
            }
        }
    }
}
