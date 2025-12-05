// Assets/Scripts/Gliding/ParagliderController.cs
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class ParagliderController : MonoBehaviour
{

float landingGraceUntil;
    [Header("Launch")]
    public float minLaunchSpeed = 9f;
    [Header("Bindings")]
    public Rigidbody rb;                        // assign in inspector (auto-fills in Reset)
    public Transform groundProbe;               // optional; else uses transform
    public LayerMask groundMask = ~0;

    [Header("Flight Params")]
    public float wingArea = 26f;
    public float baseLiftCoeff = 0.8f;          // CL at trim
    public float baseDragCoeff = 0.08f;         // CD at trim
    public float brakeDragCoeff = 0.12f;        // extra drag at full brake
    public float maxBankDeg = 45f;
    public float turnResponse = 2.0f;
    public float pitchResponse = 1.5f;
    public float minTrim = -0.3f;               // nose down (faster)
    public float maxTrim = 0.4f;               // nose up (slower)
    public float stallSpeed = 7f;
    public float flareBoost = 0.6f;
    public float groundExitHeight = 2.5f;

    [Header("Atmosphere")]
    public float airDensity = 1.225f;
    public Vector3 constantWind = new Vector3(2f, 0f, 4f);
    public bool useSlopeUpdraft = true;
    public float ridgeLiftGain = 6f;
    public float probeDistance = 40f;

    [Header("Input")]
    public string pitchAxis = "Vertical";       // W/S
    public string turnAxis = "Horizontal";     // A/D
    public KeyCode brakeKey = KeyCode.Space;    // brake / flare

    [Header("Lifecycle")]
    public bool autoEnterOnEnable = false;      // handy for prefab testing
    public UnityEvent OnGlideBegin;
    public UnityEvent OnGlideEnd;               // call your ParaglideSwitcher.EndGlide here if you like
    public UnityEvent OnLanded;                 // raised when we detect a landing

    // runtime
    public bool Gliding { get; private set; }
    float trim;            // -1..+1
    float lastAGL;         // above ground level
    Vector3 wind;          // current wind field
    Vector3 lastVel;

    bool launchedKick;



    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnEnable()
    {
        if (autoEnterOnEnable) BeginGlide();
    }


    void BeginGlideKick()
    {
        if (!rb) return;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.WakeUp();

        // ensure initial airspeed
        Vector3 v = rb.linearVelocity;
        Vector3 fwd = transform.forward;
        if (v.magnitude < minLaunchSpeed) v = fwd * minLaunchSpeed;
#if UNITY_6000_0_OR_NEWER
    rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
        launchedKick = true;
    }

    public void BeginGlide()
    {
        if (Gliding) return;
        Gliding = true;
        trim = 0f;
        BeginGlideKick();
        OnGlideBegin?.Invoke();
    }




    public void EndGlide(bool landed = false)
    {
        if (!Gliding) return;
        Gliding = false;
        if (landed) OnLanded?.Invoke();
        OnGlideEnd?.Invoke();
    }

    void Update()
    {
        if (!Gliding) return;

        if (rb.linearVelocity.magnitude < minLaunchSpeed * 0.8f)
        {
            rb.AddTorque(Vector3.right * 2.5f, ForceMode.Acceleration); // nose-down a touch
        }
        // Input (no camera logic!)
        float pitchIn = Input.GetAxis(pitchAxis);
        trim = Mathf.Clamp(trim + pitchIn * pitchResponse * Time.deltaTime, -1f, 1f);

        // Optional: bank visual can be read from angular velocity if you want to tilt a model
        // (no camera usage here)

        if (Input.GetKeyDown(KeyCode.V))
            Debug.Log($"AGL={lastAGL:F1}  vs={Vector3.Dot(rb.linearVelocity, Vector3.up):F1}");

    }

    void FixedUpdate()
    {
        Debug.Log($"[Glider] BeginGlide() -> Gliding={Gliding}");
        Debug.Log($"[Glider] Gliding={Gliding} v={rb.linearVelocity.magnitude:F1} vs={Vector3.Dot(rb.linearVelocity, Vector3.up):F1}");
if (Time.time >= landingGraceUntil)
{
    if (lastAGL > 0f && lastAGL < 0.5f && rb.linearVelocity.magnitude < 2.5f)
        EndGlide(landed: true);
}


        if (!Gliding || !rb)
        {
            Debug.Log($"RB :{rb} gliding:{Gliding} v:{rb.linearVelocity.magnitude:F1} vs:{Vector3.Dot(rb.linearVelocity, Vector3.up):F1}");

            return;
        }

        if (Time.time < 2f && (int)(Time.time * 10) % 10 == 0) // ~10 logs
            Debug.Log($"RB kinematic:{rb.isKinematic} grav:{rb.useGravity} v:{rb.linearVelocity.magnitude:F1} vs:{Vector3.Dot(rb.linearVelocity, Vector3.up):F1}");

        // One-time speed kick if we spawned from rest
        if (!launchedKick)
        {
            if (rb.linearVelocity.magnitude < minLaunchSpeed * 0.8f)
            {
                rb.AddForce(transform.forward * minLaunchSpeed, ForceMode.VelocityChange);
            }
            launchedKick = true;
        }



        // Resolve wind (constant; extend to thermals if you add them)
        wind = constantWind;

        // Apparent wind / relative airflow
        Vector3 relV = rb.linearVelocity - wind;
        float speed = relV.magnitude;
        Vector3 velDir = (speed > 0.01f) ? relV / speed : transform.forward;

        // Wing axes
        Vector3 wingForward = transform.forward;
        Vector3 wingRight = transform.right;

        // Trim → affect CL/CD
        float trimOffset = Mathf.Lerp(minTrim, maxTrim, (trim + 1f) * 0.5f);
        float effectiveCL = baseLiftCoeff * (1f + trimOffset);
        float effectiveCD = baseDragCoeff * (1f + Mathf.Max(0f, trim) * 0.6f);

        bool braking = Input.GetKey(brakeKey);
        if (braking) { effectiveCD += brakeDragCoeff; effectiveCL *= 0.9f; }

        // Dynamic pressure
        float q = 0.5f * airDensity * speed * speed;

        // Lift perpendicular to flow in wing plane
        Vector3 liftDir = Vector3.Cross(velDir, wingRight).normalized;
        Vector3 lift = liftDir * (q * wingArea * effectiveCL);
        Vector3 drag = -velDir * (q * wingArea * effectiveCD);

        // Stall softness
        if (speed < stallSpeed)
        {
            float t = Mathf.InverseLerp(0f, stallSpeed, speed);
            lift *= Mathf.Clamp01(t);
            drag += Vector3.down * (1f - t) * q * 0.3f;
        }

        // Ridge / slope updraft
        if (useSlopeUpdraft && Physics.Raycast(ProbeOrigin(), Vector3.down, out var hit, probeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 n = hit.normal;
            Vector3 windDir = wind.sqrMagnitude > 0.01f ? wind.normalized : Vector3.zero;
            float facing = Vector3.Dot(n, -windDir);
            if (facing > 0.1f)
            {
                float slope = Mathf.Clamp01(1f - Vector3.Dot(n, Vector3.up));
                lift += Vector3.up * (facing * slope * ridgeLiftGain);
            }
            lastAGL = hit.distance;
        }

        // Landing flare
        if (braking && lastAGL > 0f && lastAGL < groundExitHeight * 1.2f)
            lift += Vector3.up * (q * flareBoost);

        // Apply forces
        rb.AddForce(lift + drag, ForceMode.Force);

        // Yaw/turn input → torque
        float turnIn = Input.GetAxis(turnAxis);
        rb.AddTorque(Vector3.up * turnIn * turnResponse, ForceMode.Acceleration);

        // Softly align body with airflow
        if (speed > 0.5f)
        {
            Quaternion targetRot = Quaternion.LookRotation(Vector3.Lerp(wingForward, velDir, 0.2f), Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 2f));
        }

        // Auto-end when landed (low speed + near ground)
        lastVel = rb.linearVelocity;
        if (lastAGL > 0f && lastAGL < 0.5f && rb.linearVelocity.magnitude < 2.5f)
            EndGlide(landed: true);
    }

    Vector3 ProbeOrigin() => groundProbe ? groundProbe.position : transform.position;


    void OnDrawGizmosSelected()
    {
        if (Gliding)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ProbeOrigin(), ProbeOrigin() + Vector3.down * probeDistance);
        }
        var o = ProbeOrigin();
        Gizmos.DrawLine(o, o + Vector3.down * probeDistance);



    }
}
