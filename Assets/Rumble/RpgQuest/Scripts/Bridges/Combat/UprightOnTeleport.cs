using UnityEngine;

[DisallowMultipleComponent]
public class UprightOnTeleport : MonoBehaviour
{
    [Header("What to fix")]
    public bool clearTiltToWorldUp = true;      // zero X/Z rotation (keep yaw)
    public bool unparentIfMounted = true;      // detach from any seat/bone parent
    public bool normalizeScale = true;      // set root scale to (1,1,1)
    public bool alignToGround = false;     // align up to hit.normal instead of world up
    public float groundRayDistance = 2.0f;      // raycast down distance
    public LayerMask groundMask = ~0;        // what counts as ground

    [Header("Timing")]
    public bool alsoRunInAwake = true;      // fix at spawn
    public bool alsoRunNextFixed = true;      // re-fix one physics frame later

    Transform _t;
    Rigidbody _rb;

    void Awake()
    {
        _t = transform;
        _rb = GetComponent<Rigidbody>();
        if (alsoRunInAwake) Apply();
        if (alsoRunNextFixed) StartCoroutine(FixNextFixed());
    }

    System.Collections.IEnumerator FixNextFixed()
    {
        yield return new WaitForFixedUpdate();
        Apply();
    }

    /// Call this right after any SetPositionAndRotation / mount move.
    public void OnTeleported()
    {
        Apply();
        if (alsoRunNextFixed) StartCoroutine(FixNextFixed());
    }

    public void Apply()
    {
        // 1) Detach from any rotated parent (exiting car/seat)
        if (unparentIfMounted && _t.parent != null) _t.SetParent(null, true);

        // 2) Clear rigidbody spin so physics doesn't re-tilt us
        if (_rb)
        {
            _rb.angularVelocity = Vector3.zero;
            var vel = _rb.linearVelocity;   // keep current movement
            _rb.linearVelocity = vel;
        }
        // 3) Normalize root scale (prevents skew from animated parents)
        if (normalizeScale) _t.localScale = Vector3.one;

        // 4) Rotation fix
        if (alignToGround && Physics.Raycast(_t.position + Vector3.up * 0.2f, Vector3.down, out var hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            // align to ground normal but keep yaw around world up
            var forwardProjected = Vector3.ProjectOnPlane(_t.forward, hit.normal);
            if (forwardProjected.sqrMagnitude < 0.0001f) forwardProjected = Vector3.ProjectOnPlane(_t.right, hit.normal);
            var targetRot = Quaternion.LookRotation(forwardProjected.normalized, hit.normal);
            _t.rotation = targetRot;
        }
        else if (clearTiltToWorldUp)
        {
            // zero X/Z tilt, keep yaw
            var e = _t.eulerAngles;
            _t.rotation = Quaternion.Euler(0f, e.y, 0f);
        }

   

    }
}
