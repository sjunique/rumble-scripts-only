

// ==============================
// CharacterTrailHelper.cs
// Enables/disables a TrailRenderer based on player movement speed
// ==============================
using UnityEngine;

[AddComponentMenu("FX/Character Trail Helper")] 
public class CharacterTrailHelper : MonoBehaviour
{
    public TrailRenderer trail;         // Assign a TrailRenderer (child at feet or hips looks nice)
    public Transform trackedTransform;  // Usually the player root
    public float minSpeedToEmit = 1.5f; // m/s

    private Vector3 _lastPos;

    void Reset()
    {
        trackedTransform = transform;
        trail = GetComponentInChildren<TrailRenderer>();
    }

    void Start() { _lastPos = trackedTransform ? trackedTransform.position : transform.position; }

    void Update()
    {
        if (!trackedTransform || !trail) return;
        Vector3 pos = trackedTransform.position;
        float speed = (pos - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 1e-6f);
        _lastPos = pos;
        trail.emitting = speed >= minSpeedToEmit;
    }
}

