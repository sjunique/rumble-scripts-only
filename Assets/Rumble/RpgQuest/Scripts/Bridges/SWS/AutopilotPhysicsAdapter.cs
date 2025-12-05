// Assets/Rumble/RpgQuest/Bridges/SWS/AutopilotPhysicsAdapter.cs
using UnityEngine;

public class AutopilotPhysicsAdapter : MonoBehaviour
{
    [Header("Optional helpers (auto-found if left null)")]
    public Rigidbody rb;
    public CharacterController cc;
    public Collider mainCollider;
    public SWSAnimatorBridge animBridge;
    public GroundYProjectorSmooth groundProjector;

    // snapshots
    bool snap_rb_kinematic, snap_rb_gravity;
    RigidbodyInterpolation snap_rb_interp;
    bool snap_cc_enabled, snap_anim_enabled, snap_ground_enabled;
    RigidbodyConstraints snap_rb_constraints;
    bool initialized;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!cc) cc = GetComponent<CharacterController>();
        if (!mainCollider) mainCollider = GetComponent<Collider>();
        if (!animBridge) animBridge = GetComponent<SWSAnimatorBridge>();
        if (!groundProjector) groundProjector = GetComponent<GroundYProjectorSmooth>();

        SnapshotDefaults();
    }

    void OnEnable()
    {
        if (!initialized) SnapshotDefaults();
    }

    // take the snapshot once (Awake / first EnterAutopilot)
    void SnapshotDefaults()
    {
        if (rb)
        {
            snap_rb_kinematic = rb.isKinematic;
            snap_rb_gravity = rb.useGravity;
            snap_rb_interp = rb.interpolation;
            snap_rb_constraints = rb.constraints;
        }
        if (cc) snap_cc_enabled = cc.enabled;
        if (animBridge) snap_anim_enabled = animBridge.enabled;
        if (groundProjector) snap_ground_enabled = groundProjector.enabled;
        initialized = true;
    }


    // when AP starts (unchanged except interp=None)
    public void EnterAutopilot()
    {
        if (!initialized) SnapshotDefaults();

        if (cc) cc.enabled = false;
        if (rb)
        {
            rb.isKinematic = true;                     // drive by Transform
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
        }
        if (mainCollider) mainCollider.enabled = true;

        if (animBridge) animBridge.enabled = true;
        if (groundProjector) groundProjector.enabled = true;
    }


// IMPORTANT: restore everything on exit (this turns kinematic OFF if that was your default)
public void ExitAutopilot()
{
    if (rb)
    {
        rb.isKinematic   = snap_rb_kinematic;        // <- this flips it back (often false)
        rb.useGravity    = snap_rb_gravity;
        rb.interpolation = snap_rb_interp;
        rb.constraints   = snap_rb_constraints;
        rb.WakeUp();
    }
    if (cc) cc.enabled = snap_cc_enabled;
    if (animBridge) animBridge.enabled = snap_anim_enabled;
    if (groundProjector) groundProjector.enabled = snap_ground_enabled;
}



}
