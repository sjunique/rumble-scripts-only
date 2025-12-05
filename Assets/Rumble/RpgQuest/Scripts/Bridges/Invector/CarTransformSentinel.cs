using UnityEngine;

public class CarTransformSentinel : MonoBehaviour
{
    public float logThresholdMeters = 2.0f;
    Vector3 _lastPos;
    void OnEnable(){ _lastPos = transform.position; }
    void LateUpdate()
    {
        float d = Vector3.Distance(_lastPos, transform.position);
        if (d > logThresholdMeters)
            Debug.LogWarning($"[CarSentinel] Car moved {d:F1}m this frame to {transform.position} (frame {Time.frameCount}).");
        _lastPos = transform.position;
    }
}
