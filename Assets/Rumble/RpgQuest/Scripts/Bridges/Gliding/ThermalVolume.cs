using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Gliding/Thermal Volume")]
public class ThermalVolume : MonoBehaviour
{
    public float radius = 25f;
    public float strength = 4f;   // m/s upward near center
    public AnimationCurve falloff = AnimationCurve.EaseInOut(0,1, 1,0);

    static readonly List<ThermalVolume> s_all = new();

    void OnEnable()  { if (!s_all.Contains(this)) s_all.Add(this); }
    void OnDisable() { s_all.Remove(this); }

    public static Vector3 SampleAt(Vector3 pos)
    {
        Vector3 sum = Vector3.zero;
        foreach (var t in s_all)
        {
            float d = Vector3.Distance(pos, t.transform.position);
            if (d > t.radius) continue;
            float u = 1f - Mathf.Clamp01(d / t.radius);
            float k = t.falloff.Evaluate(u);
            sum += Vector3.up * (t.strength * k);
        }
        return sum;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
