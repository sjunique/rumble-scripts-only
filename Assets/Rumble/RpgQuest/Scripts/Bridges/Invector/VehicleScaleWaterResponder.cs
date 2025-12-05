// VehicleScaleWaterResponder.cs
// Scales the vehicle when entering/exiting water. No Cinemachine references.

using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Vehicles/Vehicle Scale Water Responder")]
public class VehicleScaleWaterResponder : MonoBehaviour
{
    public enum DetectMode { TriggersOnly, ProbeLayers }

    [Header("Detection")]
    public DetectMode detectMode = DetectMode.ProbeLayers;
    [Tooltip("For TriggersOnly: treat these tags as water triggers.")]
    public string[] waterTags = { "Water" };
    [Tooltip("For ProbeLayers: which layers count as water.")]
    public LayerMask waterLayers;
    [Tooltip("Probe shape (centered at vehicle origin). Height is along Y.")]
    public float probeRadius = 0.8f;
    public float probeHeight = 1.6f;
    public float probeOffsetY = 0.2f;

    [Header("Scaling")]
    public Vector3 landScale  = new Vector3(4, 4, 4);
    public Vector3 waterScale = new Vector3(1, 1, 1);
    [Tooltip("Seconds to blend between scales (0 = instant).")]
    public float scaleBlendSeconds = 0.20f;

    [Header("Optional camera hook (kept OFF by default)")]
    public bool callWhenStateChanges = false;
    public UnityEvent onEnterWater;
    public UnityEvent onExitWater;

    [Header("Debug")]
    public bool drawProbeGizmo = true;
    public Color gizmoColorLand = new Color(0f, 1f, 0f, 0.15f);
    public Color gizmoColorWater = new Color(0f, 0.6f, 1f, 0.15f);

    bool _inWater;
    bool _blending;
    Vector3 _fromScale, _toScale;
    float _blendT;

    void Reset()
    {
        // Reasonable defaults
        waterLayers = LayerMask.GetMask("Water");
    }

    void OnEnable()
    {
        // Start in whatever state matches the environment
        _inWater = DetectWaterNow();
        transform.localScale = _inWater ? waterScale : landScale;
        _blending = false;
    }

    void Update()
    {
        // Detect state
        bool nowInWater = DetectWaterNow();
        if (nowInWater != _inWater)
        {
            _inWater = nowInWater;
            BeginBlend(_inWater ? waterScale : landScale);

            if (callWhenStateChanges)
            {
                if (_inWater) onEnterWater?.Invoke();
                else          onExitWater?.Invoke();
            }
        }

        // Blend scale
        if (_blending)
        {
            _blendT += (scaleBlendSeconds <= 0f ? 1f : Time.deltaTime / scaleBlendSeconds);
            if (_blendT >= 1f)
            {
                _blending = false;
                transform.localScale = _toScale;
            }
            else
            {
                transform.localScale = Vector3.Lerp(_fromScale, _toScale, _blendT);
            }
        }
    }

    bool DetectWaterNow()
    {
        if (detectMode == DetectMode.TriggersOnly)
        {
            // Trigger path doesn’t actively query; state changes come from OnTriggerEnter/Exit
            return _inWater;
        }
        // Probe path: a simple overlap box
        Vector3 half = new Vector3(probeRadius, probeHeight * 0.5f, probeRadius);
        Vector3 center = transform.position + new Vector3(0f, probeOffsetY + half.y, 0f);
        return Physics.CheckBox(center, half, Quaternion.identity, waterLayers, QueryTriggerInteraction.Collide);
    }

    void BeginBlend(Vector3 targetScale)
    {
        _fromScale = transform.localScale;
        _toScale   = targetScale;
        _blendT    = 0f;
        _blending  = (_fromScale != _toScale);
        if (!_blending) transform.localScale = _toScale;
    }

    // Trigger mode support (optional)
    void OnTriggerEnter(Collider other)
    {
        if (detectMode != DetectMode.TriggersOnly) return;
        if (MatchesWaterTag(other.tag))
        {
            _inWater = true;
            BeginBlend(waterScale);
            if (callWhenStateChanges) onEnterWater?.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (detectMode != DetectMode.TriggersOnly) return;
        if (MatchesWaterTag(other.tag))
        {
            _inWater = false;
            BeginBlend(landScale);
            if (callWhenStateChanges) onExitWater?.Invoke();
        }
    }

    bool MatchesWaterTag(string t)
    {
        if (string.IsNullOrEmpty(t)) return false;
        for (int i = 0; i < waterTags.Length; i++)
            if (!string.IsNullOrEmpty(waterTags[i]) && t == waterTags[i]) return true;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawProbeGizmo) return;
        Vector3 half = new Vector3(probeRadius, probeHeight * 0.5f, probeRadius);
        Vector3 center = transform.position + new Vector3(0f, probeOffsetY + half.y, 0f);
        Gizmos.color = _inWater ? gizmoColorWater : gizmoColorLand;
        Gizmos.DrawCube(center, half * 2f);
    }
}
