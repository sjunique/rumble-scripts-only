using UnityEngine;

using UnityEngine;

[DefaultExecutionOrder(-10)]
public class AIHearingSensor : MonoBehaviour
{
    [Header("Hearing")]
    [Tooltip("Extra hearing sensitivity multiplier (1 = neutral).")]
    public float sensitivity = 1f;
    [Tooltip("How long to remember a heard sound (sec).")]
    public float memorySeconds = 5f;

    [Header("Occlusion")]
    public LayerMask obstructionMask = ~0;
    [Tooltip("Raycast height offset from AI pivot (meters).")]
    public float headHeight = 1.7f;

    public bool HasHeardRecently => Time.time - _lastHeardTime <= memorySeconds;
    public Vector3 LastHeardPosition => _lastHeardPos;
    public float LastHeardTime => _lastHeardTime;

    Vector3 _lastHeardPos;
    float _lastHeardTime = -999f;
    Transform _t;

    void OnEnable()
    {
        _t = transform;
        SoundEmitter.OnSoundEmitted += OnSound;
    }

    void OnDisable()
    {
        SoundEmitter.OnSoundEmitted -= OnSound;
    }

    void OnSound(Vector3 pos, float radius)
    {
        var hearRadius = radius * Mathf.Max(0.01f, sensitivity);
        var myPos = _t.position + Vector3.up * headHeight;
        var flat = pos - myPos;
        var dist = flat.magnitude;
        if (dist > hearRadius) return;

        // Simple occlusion: if blocked, reduce effective radius
        if (Physics.Linecast(myPos, pos, out var hit, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            // If something blocks, consider it muffled; shrink radius by 40% as a cheap model
            if (dist > hearRadius * 0.6f) return;
        }

        _lastHeardPos = pos;
        _lastHeardTime = Time.time;
        // Optional: Debug draw
        Debug.DrawLine(myPos, pos, Color.yellow, 0.75f);
    }
}
