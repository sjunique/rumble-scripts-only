

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(InspectorRotation))]
public class RpgHoverController : MonoBehaviour
{

 [Header("Driving State")]
    public bool canDrive = false;   // <- KEY FLAG



[Header("VTOL Lift Dynamics")]
[SerializeField, Tooltip("Stronger upward force for crisp lift-off")]
private float ascendLiftForce = 12000f;

[SerializeField, Tooltip("Softer downward force for controlled landings")]
private float descendLiftForce = 4000f;

[SerializeField, Tooltip("Caps upward speed during lift-off (m/s)")]
private float maxAscentSpeed = 12f;

[SerializeField, Tooltip("Caps downward speed during descent (m/s)")]
private float maxDescentSpeed = 7f;

[SerializeField, Tooltip("Time to ramp from idle to full lift when holding the key")]
private float liftRampUpTime = 0.20f;

[SerializeField, Tooltip("Time to fade lift when releasing the key")]
private float liftRampDownTime = 0.12f;

[Header("Boost (Optional)")]
[SerializeField] private KeyCode liftBoostKey = KeyCode.LeftAlt;
[SerializeField, Tooltip("Extra punch while held")] 
private float liftBoostMultiplier = 1.35f;

[Header("Takeoff Assist")]
[SerializeField, Tooltip("Short burst on quick-tap of Ascend to break static friction")]
private bool quickTapTakeoff = true;
[SerializeField, Tooltip("Tap window (seconds) recognized as a quick-tap")]
private float quickTapWindow = 0.18f;
[SerializeField, Tooltip("Extra burst strength on quick-tap")]
private float quickTapMultiplier = 1.6f;
[SerializeField, Tooltip("Duration of the burst (seconds)")]
private float quickTapDuration = 0.22f;

// runtime
float _liftFactor;            // 0..1 ramp for lift strength
float _tapTimer;              // counts while ascend is quickly tapped
bool  _tapActive;
bool  _ascendPrev;










    [Header("VTOL Settings")]
    [Tooltip("How fast the vehicle lifts off or descends when in VTOL mode")]
    [SerializeField] private float verticalLiftForce = 8000f;
    [SerializeField] private float maxVTOLHeight   = 20f;
    [SerializeField] private KeyCode ascendKey      = KeyCode.Space;
    [SerializeField] private KeyCode descendKey     = KeyCode.C;

    // New tuning knobs:
    [SerializeField][Tooltip("Max downward speed (m/s) when descending")] 
    private float maxDescendSpeed    = 10f;
    [SerializeField][Tooltip("How close to ground before we snap up (m)")] 
    private float groundSnapBuffer   = 0.1f;

    private bool isVTOL = false;

    readonly KeyCode LeftTurn = KeyCode.A;
    readonly KeyCode RightTurn = KeyCode.D;
    readonly KeyCode TiltUp = KeyCode.LeftControl;
    readonly KeyCode TiltDown = KeyCode.LeftShift;

    [Tooltip("Tilt angle along the z-axis")]
    [SerializeField][Range(0, 100)] float MaxSideTiltAngle = 60;

    [Tooltip("The more the velocity rate, the faster it starts moving.")]
    [SerializeField][Range(1, 10)] float VelocityChangeRate = 1f;

    [Tooltip("The length determines when the hover vehicle will start flying automatically. If the length is high, then it will detect the ground, if the length is low, then it will be easier for the vehicle to go airborne.")]
    [SerializeField][Range(1, 10)] float GroundDetectRayLength = 2f;

    InspectorRotation Inspector;

    [Tooltip("The layer of the ground. Make sure to keep your vehicles layer different from the ground layer otherwise the ray might collide with your vehicle instead.")]
    [SerializeField] LayerMask GroundLayer;

    [Tooltip("The higher the force, the faster the hover vehicle goes.")]
    [SerializeField] float Force = 100;
    [Tooltip("Determines the speed of rotation")]
    [SerializeField] float RotationSpeed = 1500.0f;

    protected Rigidbody Rigidbody;

    float MaxVerticalTiltAngle = 40;
    float SideReturnSpeed = 1;
    float VerticalReturnSpeed = 0.5f;
    float VerticalVal = 0.0f;
    float HorizontalVal = 0.0f;
    float PitchVal = 0f;

    float MoveTimer = 0;
    const int Multiplier = 100;

    const float Threshold = 0.1f;
    const float RotationThreshold = 1.5f;

    [Tooltip("Increase this value if your vehicle is heavy and decrease it if light.")]
    public float HoverForce = 1000f;
    [Tooltip("The height above the ground you want your vehicle to hover.")]
    public float HoverHeight = 2.5f;

    [Tooltip("All the points from where the hover force is generated.")]
    public GameObject[] EnginePoints;

    bool HoverOnGround = true;

    bool TiltDownButton = false;
    bool TiltUpButton = false;

    float Pitch = 0;

    // ─────────────────────────────────────────────────────────────
    //                      AUDIO / LIGHTS / VFX
    // ─────────────────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("Continuous loop for idle hum")]
    public AudioClip engineIdle;
    [Tooltip("Loop for forward acceleration/boost")]
    public AudioClip engineAccel;
    [Tooltip("Loop for reverse/deceleration")]
    public AudioClip engineDecel;

    [Range(0f,1f)] public float idleVolume = 0.35f;
    [Range(0f,1f)] public float accelVolume = 0.9f;
    [Range(0f,1f)] public float decelVolume = 0.8f;
    public Vector2 enginePitchRange = new Vector2(0.85f, 1.35f);

    [Header("Lights")]
    public Light[] headlights;            // Spot lights at front
    public Light[] tailLights;            // Red point/spot lights at rear
    public KeyCode toggleHeadlightsKey = KeyCode.H;
    public bool headlightsOn = true;
    [Range(0f,8f)] public float headlightIntensityOn = 2.2f;
    [Range(0f,8f)] public float headlightIntensityOff = 0f;
    [Range(0f,8f)] public float tailLightIdleIntensity = 0.6f;
    [Range(0f,8f)] public float tailLightBrakeIntensity = 3.5f;

    [Header("Thrusters / Particles")]
    [Tooltip("Rear thrusters fire when accelerating forward.")]
    public ParticleSystem[] rearThrusters;
    [Tooltip("Bottom jets fire when ascending; softly when hovering (VTOL).")]
    public ParticleSystem[] bottomThrusters;
    public float rearThrusterRateMax = 60f;
    public float bottomThrusterRateMax = 80f;
    public float bottomThrusterRateIdle = 10f;

    [Header("Audio Debug")]
    public bool debugForce2DAudio = false;
    public bool debugLogAudioState = false;

    // runtime audio sources
    AudioSource _srcIdle, _srcAccel, _srcDecel;

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        Rigidbody = GetComponentInParent<Rigidbody>();
        Inspector = GetComponentInParent<InspectorRotation>();
    }

    void Awake()
    {
        // Create audio sources (non-intrusive)
        PrepareAudioSource(ref _srcIdle,  "EngineIdle",  engineIdle,  true, idleVolume);
        PrepareAudioSource(ref _srcAccel, "EngineAccel", engineAccel, true, 0f);
        PrepareAudioSource(ref _srcDecel, "EngineDecel", engineDecel, true, 0f);
    }

private void VTOLMovement()
{
    // Inputs
    bool up    = Input.GetKey(ascendKey);
    bool down  = Input.GetKey(descendKey);
    bool boost = Input.GetKey(liftBoostKey);

    float vCmd = (up ? 1f : 0f) - (down ? 1f : 0f);  // +1 ascend, -1 descend

    // ── Quick-tap takeoff (short boost if you just tap ascend) ─────────────────
    if (quickTapTakeoff)
    {
        if (up && !_ascendPrev) { _tapTimer = 0f; _tapActive = true; }
        if (_tapActive)
        {
            _tapTimer += Time.fixedDeltaTime;
            if (_tapTimer > quickTapDuration) _tapActive = false;
        }
        if (!up && _ascendPrev && _tapTimer <= quickTapWindow)
        {
            // released quickly → keep burst running until quickTapDuration elapses
            _tapActive = true;
        }
        _ascendPrev = up;
    }

    // ── Lift factor ramp (smooth engagement / release) ─────────────────────────
    float target = Mathf.Abs(vCmd); // 0 or 1
    float rampUp   = (liftRampUpTime   > 0f) ? (Time.fixedDeltaTime / liftRampUpTime)   : 1f;
    float rampDown = (liftRampDownTime > 0f) ? (Time.fixedDeltaTime / liftRampDownTime) : 1f;
    _liftFactor = Mathf.MoveTowards(_liftFactor, target, (target > _liftFactor ? rampUp : rampDown));
    float boostMul = (boost ? liftBoostMultiplier : 1f);
    if (quickTapTakeoff && _tapActive) boostMul = Mathf.Max(boostMul, quickTapMultiplier);

    // ── Compute vertical accel (split forces for up/down) ──────────────────────
    float accelUp   = ascendLiftForce;
    float accelDown = descendLiftForce;
    float accel = 0f;
    if (vCmd > 0f)      accel =  vCmd * _liftFactor * accelUp  * boostMul;  // ascend
    else if (vCmd < 0f) accel =  vCmd * _liftFactor * accelDown;            // descend

    // Apply vertical acceleration
    if (accel != 0f)
        Rigidbody.AddForce(Vector3.up * accel, ForceMode.Acceleration);

    // Ceiling clamp
    if (transform.position.y > maxVTOLHeight && vCmd > 0f)
    {
        var v = Rigidbody.linearVelocity;
        if (v.y > 0f) v.y = 0f;
        Rigidbody.linearVelocity = v;
    }

    // Speed caps (up/down)
    var vel = Rigidbody.linearVelocity;
    if (vel.y > 0f) vel.y = Mathf.Min(vel.y, maxAscentSpeed);
    else            vel.y = Mathf.Max(vel.y, -maxDescentSpeed);
    Rigidbody.linearVelocity = vel;

    // Normal mid-air steering while VTOLing
    LinearMidAirMovement();
    ForceTurn();
}



    private void VTOLMovementold()
    {
        // 1) Read inputs
        float up = Input.GetKey(ascendKey) ? 1f : 0f;
        float down = Input.GetKey(descendKey) ? 1f : 0f;
        float vCmd = up - down;  // +1 = ascend, –1 = descend

        // 2) Apply raw vertical acceleration
        Rigidbody.AddForce(Vector3.up * vCmd * verticalLiftForce, ForceMode.Acceleration);

        // 3) Ceiling clamp
        if (transform.position.y > maxVTOLHeight && up > 0f)
        {
            var v = Rigidbody.linearVelocity;
            if (v.y > 0f) v.y = 0f;
            Rigidbody.linearVelocity = v;
        }

        // 4) Descent control & ground snap
        if (vCmd < 0f)
        {
            // Clamp your downward speed
            var vel = Rigidbody.linearVelocity;
            vel.y = Mathf.Max(vel.y, -maxDescendSpeed);
            Rigidbody.linearVelocity = vel;

            // Raycast from center down to see your true height
            if (Physics.Raycast(transform.position, Vector3.down, out var hit, HoverHeight + 1f, GroundLayer))
            {
                float distToGround = hit.distance;
                float minAllowed = HoverHeight + groundSnapBuffer;
                if (distToGround <= minAllowed)
                {
                    // Snap to just above ground and kill vertical motion
                    float targetY = hit.point.y + HoverHeight;
                    transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                    vel = Rigidbody.linearVelocity;
                    vel.y = 0f;
                    Rigidbody.linearVelocity = vel;
                    return;  // skip steering this frame
                }
            }
        }

        // 5) Allow normal mid-air steering
        LinearMidAirMovement();
        ForceTurn();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))      { Pitch = 1; }
        else if (Input.GetKey(KeyCode.LeftControl)){ Pitch = -1; }
        else                                      { Pitch = 0; }

        // VTOL state = any lift/descend input is held
        bool ascendHeld  = Input.GetKey(ascendKey);
        bool descendHeld = Input.GetKey(descendKey);
        isVTOL = ascendHeld || descendHeld;

        // gravity only when *not* VTOL
        Rigidbody.useGravity = !isVTOL;

        if (Input.GetKeyDown(toggleHeadlightsKey))
            headlightsOn = !headlightsOn;

        HoverMidAirUpdate();
    }

    void FixedUpdate()
    {
        if (isVTOL)
            VTOLMovement();
        else
            Controller();

        // 🔊💡🔥 Update audio + lights + thrusters (does not affect physics)
        UpdateAudioAndFX();
    }

    private void Controller()
    {
     if (!canDrive)
            return; 

        LinearMidAirMovement();
        ForceTurn();

        if (HoverOnGround)
            GroundHover();

        GroundCheck();
    }

    /// <summary>
    /// Mid air controller. All the inputs from the user is taken from here...
    /// </summary>
    private void HoverMidAirUpdate()
    {
        //vertical input from user.
        VerticalVal = Input.GetAxis("Vertical");

        //horizontal input from the user.
        HorizontalVal = Mathf.Lerp(HorizontalVal, Input.GetAxis("Horizontal"), Time.deltaTime * 2);

        //the pitch value which controls the pitch of the vehicle.
        PitchVal = Mathf.Lerp(PitchVal, Pitch, Time.deltaTime * 5);

        //tilt buttons
        TiltDownButton = Input.GetKey(TiltDown);
        TiltUpButton   = Input.GetKey(TiltUp);

        if (PitchVal < -Threshold)
            HoverOnGround = false;
    }

    /// <summary>
    /// The vehicle is turned using Torque and the max angle depends on the threshold.
    /// </summary>
    private void ForceTurn()
    {
        Rigidbody.AddTorque(Vector3.up * RotationSpeed * Time.deltaTime * HorizontalVal, ForceMode.Acceleration);

        if (!HoverOnGround)
        {
            if (Inspector.Z > -MaxSideTiltAngle && Inspector.Z < MaxSideTiltAngle)
                Rigidbody.AddTorque(transform.forward * RotationSpeed * Time.deltaTime * -HorizontalVal * SideReturnSpeed, ForceMode.Acceleration);
        }

        if (!Input.GetKey(LeftTurn))
        {
            if (!HoverOnGround)
                if (Inspector.Z > RotationThreshold)
                    Rigidbody.AddTorque(-transform.forward * RotationSpeed * Time.deltaTime, ForceMode.Acceleration);
        }

        if (!Input.GetKey(RightTurn))
        {
            if (!HoverOnGround)
                if (Inspector.Z < -RotationThreshold)
                    Rigidbody.AddTorque(transform.forward * RotationSpeed * Time.deltaTime, ForceMode.Acceleration);
        }

        if (!HoverOnGround)
        {
            if (Inspector.X > -MaxVerticalTiltAngle && Inspector.X < MaxVerticalTiltAngle)
                Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * PitchVal / 2, ForceMode.Acceleration);
        }
        else
        {
            Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * PitchVal / 2, ForceMode.Acceleration);
        }

        if (!TiltDownButton && !TiltUpButton)
        {
            if (!HoverOnGround)
            {
                if (Inspector.X > RotationThreshold)
                    Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * -VerticalReturnSpeed, ForceMode.Acceleration);

                if (Inspector.X < -RotationThreshold)
                    Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * VerticalReturnSpeed, ForceMode.Acceleration);
            }
        }
    }

    /// <summary>
    /// Controls all the movement from here. Movement is applied using Rigidbody.Velocity..
    /// </summary>
    private void LinearMidAirMovement()
    {
        if (VerticalVal > Threshold)
        {
            Vector3 TargetVelocity = Force * Multiplier * Time.fixedDeltaTime * transform.forward;
            if (Rigidbody.isKinematic)
                Rigidbody.MovePosition(Rigidbody.position + Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f) * Time.fixedDeltaTime);
            else
                Rigidbody.linearVelocity = Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f);
            MoveTimer = 0;
        }

        if (VerticalVal < -Threshold)
        {
            Vector3 TargetVelocity = Force * Multiplier * Time.fixedDeltaTime * -transform.forward;
            if (Rigidbody.isKinematic)
                Rigidbody.MovePosition(Rigidbody.position + Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f) * Time.fixedDeltaTime);
            else
                Rigidbody.linearVelocity = Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f);
            MoveTimer = 0;
        }

        if (VerticalVal == 0f)
        {
            MoveTimer += Time.deltaTime / 2.0f;
            Vector3 lerpedVel = Vector3.Lerp(Rigidbody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * MoveTimer * VelocityChangeRate);
            if (Rigidbody.isKinematic)
                Rigidbody.MovePosition(Rigidbody.position + lerpedVel * Time.fixedDeltaTime);
            else
                Rigidbody.linearVelocity = lerpedVel;
        }
    }

    /// <summary>
    /// Checks if the ground is in vicinity.
    /// </summary>
    private void GroundCheck()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, HoverHeight + GroundDetectRayLength, GroundLayer))
        {
            HoverOnGround = true;
            Rigidbody.useGravity = true;
        }
        else
        {
            HoverOnGround = false;
            Rigidbody.useGravity = false;
        }
    }

    /// <summary>
    /// Hover mechanics of the vehicle when close to the ground.
    /// </summary>
    private void GroundHover()
    {
        RaycastHit hit;
        for (int i = 0; i < EnginePoints.Length; i++)
        {
            var hoverPoint = EnginePoints[i];
            if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out hit, HoverHeight, GroundLayer))
            {
                Rigidbody.AddForceAtPosition(Vector3.up * HoverForce * (1.0f - (hit.distance / HoverHeight)), hoverPoint.transform.position);
            }
            else
            {
                if (HoverOnGround)
                {
                    if (transform.position.y > hoverPoint.transform.position.y)
                        Rigidbody.AddForceAtPosition(transform.up * HoverForce, transform.position);
                    else
                        Rigidbody.AddForceAtPosition(hoverPoint.transform.up * -HoverForce, hoverPoint.transform.position);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //                      AUDIO + LIGHTS + VFX
    // ─────────────────────────────────────────────────────────────
    void PrepareAudioSource(ref AudioSource src, string goName, AudioClip clip, bool loop, float vol)
    {
        if (!clip) return;
        if (!src)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.rolloffMode = AudioRolloffMode.Linear;
        }
        src.clip = clip;
        src.volume = vol;
        src.spatialBlend = debugForce2DAudio ? 0f : 0.7f; // partly 3D but audible
        src.minDistance = 4f;
        src.maxDistance = 60f;
        if (!src.isPlaying) { src.Play(); if (debugLogAudioState) Debug.Log($"[HoverAudio] {goName} start"); }
    }

    void UpdateAudioAndFX()
    {
        // Inputs/state
        float throttle = Mathf.Clamp(VerticalVal, -1f, 1f);
        Vector3 v3D = Rigidbody ? Rigidbody.linearVelocity : Vector3.zero;  // use 3D velocity to estimate speed
        float fwdSpeed = Vector3.Dot(v3D, transform.forward);
        float speed01 = Mathf.Clamp01(Mathf.Abs(fwdSpeed) / (Force * Multiplier * Time.fixedDeltaTime + 0.01f));

        bool ascending = Input.GetKey(ascendKey);
        bool descending = Input.GetKey(descendKey);

        // AUDIO
        if (_srcIdle)  _srcIdle.volume  = engineIdle ? idleVolume : 0f;

        if (_srcAccel)
        {
            float a = Mathf.Max(0f, throttle);
            _srcAccel.volume = accelVolume * a;
            _srcAccel.pitch  = Mathf.Lerp(enginePitchRange.x, enginePitchRange.y, Mathf.Max(a, speed01));
            if (engineAccel && !_srcAccel.isPlaying) _srcAccel.Play();
        }

        if (_srcDecel)
        {
            float d = Mathf.Max(0f, -throttle);
            float braking = 0f;
            if (Mathf.Sign(fwdSpeed) > 0 && throttle < 0) braking = Mathf.Clamp01(-throttle);
            if (Mathf.Sign(fwdSpeed) < 0 && throttle > 0) braking = Mathf.Clamp01(throttle);

            float decelMix = Mathf.Clamp01(d + 0.5f * braking);
            _srcDecel.volume = decelVolume * decelMix;
            _srcDecel.pitch  = Mathf.Lerp(enginePitchRange.x, enginePitchRange.y, Mathf.Max(decelMix, speed01 * 0.5f));
            if (engineDecel && !_srcDecel.isPlaying) _srcDecel.Play();
        }

        // LIGHTS
        if (headlights != null)
        {
            float hI = headlightsOn ? headlightIntensityOn : headlightIntensityOff;
            foreach (var l in headlights) if (l) l.intensity = hI;
        }
        if (tailLights != null)
        {
            bool brakingNow = throttle < -0.05f || descending;
            float target = brakingNow ? tailLightBrakeIntensity : tailLightIdleIntensity;
            foreach (var l in tailLights) if (l) l.intensity = Mathf.Lerp(l.intensity, target, 0.25f);
        }

        // THRUSTERS
        float rearRate = Mathf.Lerp(0f, rearThrusterRateMax, Mathf.Clamp01(throttle));
        SetEmission(rearThrusters, rearRate);

        float bottomRate = 0f;
        if (ascending) bottomRate = bottomThrusterRateMax;
        else if (isVTOL) bottomRate = Mathf.Max(bottomThrusterRateIdle, bottomThrusterRateMax * 0.25f);
        else if (!HoverOnGround) bottomRate = bottomThrusterRateIdle;
        else bottomRate = 0f;
        SetEmission(bottomThrusters, bottomRate);
    }

    void SetEmission(ParticleSystem[] systems, float rate)
    {
        if (systems == null) return;
        foreach (var ps in systems)
        {
            if (!ps) continue;
            var em = ps.emission;
            var r  = em.rateOverTime;
            r.constant = rate;
            em.rateOverTime = r;

            if (rate > 0f) { if (!ps.isPlaying) ps.Play(); }
            else           { if (ps.isPlaying)  ps.Stop(); }
        }
    }
}


// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// [RequireComponent(typeof(Rigidbody))]
// [RequireComponent(typeof(BoxCollider))]
// [RequireComponent(typeof(InspectorRotation))]
// public class RpgHoverController : MonoBehaviour
// {

// [Header("VTOL Lift Dynamics")]
// [SerializeField, Tooltip("Stronger upward force for crisp lift-off")]
// private float ascendLiftForce = 12000f;

// [SerializeField, Tooltip("Softer downward force for controlled landings")]
// private float descendLiftForce = 4000f;

// [SerializeField, Tooltip("Caps upward speed during lift-off (m/s)")]
// private float maxAscentSpeed = 12f;

// [SerializeField, Tooltip("Caps downward speed during descent (m/s)")]
// private float maxDescentSpeed = 7f;

// [SerializeField, Tooltip("Time to ramp from idle to full lift when holding the key")]
// private float liftRampUpTime = 0.20f;

// [SerializeField, Tooltip("Time to fade lift when releasing the key")]
// private float liftRampDownTime = 0.12f;

// [Header("Boost (Optional)")]
// [SerializeField] private KeyCode liftBoostKey = KeyCode.LeftAlt;
// [SerializeField, Tooltip("Extra punch while held")] 
// private float liftBoostMultiplier = 1.35f;

// [Header("Takeoff Assist")]
// [SerializeField, Tooltip("Short burst on quick-tap of Ascend to break static friction")]
// private bool quickTapTakeoff = true;
// [SerializeField, Tooltip("Tap window (seconds) recognized as a quick-tap")]
// private float quickTapWindow = 0.18f;
// [SerializeField, Tooltip("Extra burst strength on quick-tap")]
// private float quickTapMultiplier = 1.6f;
// [SerializeField, Tooltip("Duration of the burst (seconds)")]
// private float quickTapDuration = 0.22f;

// // runtime
// float _liftFactor;            // 0..1 ramp for lift strength
// float _tapTimer;              // counts while ascend is quickly tapped
// bool  _tapActive;
// bool  _ascendPrev;










//     [Header("VTOL Settings")]
//     [Tooltip("How fast the vehicle lifts off or descends when in VTOL mode")]
//     [SerializeField] private float verticalLiftForce = 8000f;
//     [SerializeField] private float maxVTOLHeight   = 20f;
//     [SerializeField] private KeyCode ascendKey      = KeyCode.Space;
//     [SerializeField] private KeyCode descendKey     = KeyCode.C;

//     // New tuning knobs:
//     [SerializeField][Tooltip("Max downward speed (m/s) when descending")] 
//     private float maxDescendSpeed    = 10f;
//     [SerializeField][Tooltip("How close to ground before we snap up (m)")] 
//     private float groundSnapBuffer   = 0.1f;

//     private bool isVTOL = false;

//     readonly KeyCode LeftTurn = KeyCode.A;
//     readonly KeyCode RightTurn = KeyCode.D;
//     readonly KeyCode TiltUp = KeyCode.LeftControl;
//     readonly KeyCode TiltDown = KeyCode.LeftShift;

//     [Tooltip("Tilt angle along the z-axis")]
//     [SerializeField][Range(0, 100)] float MaxSideTiltAngle = 60;

//     [Tooltip("The more the velocity rate, the faster it starts moving.")]
//     [SerializeField][Range(1, 10)] float VelocityChangeRate = 1f;

//     [Tooltip("The length determines when the hover vehicle will start flying automatically. If the length is high, then it will detect the ground, if the length is low, then it will be easier for the vehicle to go airborne.")]
//     [SerializeField][Range(1, 10)] float GroundDetectRayLength = 2f;

//     InspectorRotation Inspector;

//     [Tooltip("The layer of the ground. Make sure to keep your vehicles layer different from the ground layer otherwise the ray might collide with your vehicle instead.")]
//     [SerializeField] LayerMask GroundLayer;

//     [Tooltip("The higher the force, the faster the hover vehicle goes.")]
//     [SerializeField] float Force = 100;
//     [Tooltip("Determines the speed of rotation")]
//     [SerializeField] float RotationSpeed = 1500.0f;

//     protected Rigidbody Rigidbody;

//     float MaxVerticalTiltAngle = 40;
//     float SideReturnSpeed = 1;
//     float VerticalReturnSpeed = 0.5f;
//     float VerticalVal = 0.0f;
//     float HorizontalVal = 0.0f;
//     float PitchVal = 0f;

//     float MoveTimer = 0;
//     const int Multiplier = 100;

//     const float Threshold = 0.1f;
//     const float RotationThreshold = 1.5f;

//     [Tooltip("Increase this value if your vehicle is heavy and decrease it if light.")]
//     public float HoverForce = 1000f;
//     [Tooltip("The height above the ground you want your vehicle to hover.")]
//     public float HoverHeight = 2.5f;

//     [Tooltip("All the points from where the hover force is generated.")]
//     public GameObject[] EnginePoints;

//     bool HoverOnGround = true;

//     bool TiltDownButton = false;
//     bool TiltUpButton = false;

//     float Pitch = 0;

//     // ─────────────────────────────────────────────────────────────
//     //                      AUDIO / LIGHTS / VFX
//     // ─────────────────────────────────────────────────────────────
//     [Header("Audio")]
//     [Tooltip("Continuous loop for idle hum")]
//     public AudioClip engineIdle;
//     [Tooltip("Loop for forward acceleration/boost")]
//     public AudioClip engineAccel;
//     [Tooltip("Loop for reverse/deceleration")]
//     public AudioClip engineDecel;

//     [Range(0f,1f)] public float idleVolume = 0.35f;
//     [Range(0f,1f)] public float accelVolume = 0.9f;
//     [Range(0f,1f)] public float decelVolume = 0.8f;
//     public Vector2 enginePitchRange = new Vector2(0.85f, 1.35f);

//     [Header("Lights")]
//     public Light[] headlights;            // Spot lights at front
//     public Light[] tailLights;            // Red point/spot lights at rear
//     public KeyCode toggleHeadlightsKey = KeyCode.H;
//     public bool headlightsOn = true;
//     [Range(0f,8f)] public float headlightIntensityOn = 2.2f;
//     [Range(0f,8f)] public float headlightIntensityOff = 0f;
//     [Range(0f,8f)] public float tailLightIdleIntensity = 0.6f;
//     [Range(0f,8f)] public float tailLightBrakeIntensity = 3.5f;

//     [Header("Thrusters / Particles")]
//     [Tooltip("Rear thrusters fire when accelerating forward.")]
//     public ParticleSystem[] rearThrusters;
//     [Tooltip("Bottom jets fire when ascending; softly when hovering (VTOL).")]
//     public ParticleSystem[] bottomThrusters;
//     public float rearThrusterRateMax = 60f;
//     public float bottomThrusterRateMax = 80f;
//     public float bottomThrusterRateIdle = 10f;

//     [Header("Audio Debug")]
//     public bool debugForce2DAudio = false;
//     public bool debugLogAudioState = false;

//     // runtime audio sources
//     AudioSource _srcIdle, _srcAccel, _srcDecel;

//     // ─────────────────────────────────────────────────────────────

//     void Start()
//     {
//         Rigidbody = GetComponentInParent<Rigidbody>();
//         Inspector = GetComponentInParent<InspectorRotation>();
//     }

//     void Awake()
//     {
//         // Create audio sources (non-intrusive)
//         PrepareAudioSource(ref _srcIdle,  "EngineIdle",  engineIdle,  true, idleVolume);
//         PrepareAudioSource(ref _srcAccel, "EngineAccel", engineAccel, true, 0f);
//         PrepareAudioSource(ref _srcDecel, "EngineDecel", engineDecel, true, 0f);
//     }

// private void VTOLMovement()
// {
//     // Inputs
//     bool up    = Input.GetKey(ascendKey);
//     bool down  = Input.GetKey(descendKey);
//     bool boost = Input.GetKey(liftBoostKey);

//     float vCmd = (up ? 1f : 0f) - (down ? 1f : 0f);  // +1 ascend, -1 descend

//     // ── Quick-tap takeoff (short boost if you just tap ascend) ─────────────────
//     if (quickTapTakeoff)
//     {
//         if (up && !_ascendPrev) { _tapTimer = 0f; _tapActive = true; }
//         if (_tapActive)
//         {
//             _tapTimer += Time.fixedDeltaTime;
//             if (_tapTimer > quickTapDuration) _tapActive = false;
//         }
//         if (!up && _ascendPrev && _tapTimer <= quickTapWindow)
//         {
//             // released quickly → keep burst running until quickTapDuration elapses
//             _tapActive = true;
//         }
//         _ascendPrev = up;
//     }

//     // ── Lift factor ramp (smooth engagement / release) ─────────────────────────
//     float target = Mathf.Abs(vCmd); // 0 or 1
//     float rampUp   = (liftRampUpTime   > 0f) ? (Time.fixedDeltaTime / liftRampUpTime)   : 1f;
//     float rampDown = (liftRampDownTime > 0f) ? (Time.fixedDeltaTime / liftRampDownTime) : 1f;
//     _liftFactor = Mathf.MoveTowards(_liftFactor, target, (target > _liftFactor ? rampUp : rampDown));
//     float boostMul = (boost ? liftBoostMultiplier : 1f);
//     if (quickTapTakeoff && _tapActive) boostMul = Mathf.Max(boostMul, quickTapMultiplier);

//     // ── Compute vertical accel (split forces for up/down) ──────────────────────
//     float accelUp   = ascendLiftForce;
//     float accelDown = descendLiftForce;
//     float accel = 0f;
//     if (vCmd > 0f)      accel =  vCmd * _liftFactor * accelUp  * boostMul;  // ascend
//     else if (vCmd < 0f) accel =  vCmd * _liftFactor * accelDown;            // descend

//     // Apply vertical acceleration
//     if (accel != 0f)
//         Rigidbody.AddForce(Vector3.up * accel, ForceMode.Acceleration);

//     // Ceiling clamp
//     if (transform.position.y > maxVTOLHeight && vCmd > 0f)
//     {
//         var v = Rigidbody.linearVelocity;
//         if (v.y > 0f) v.y = 0f;
//         Rigidbody.linearVelocity = v;
//     }

//     // Speed caps (up/down)
//     var vel = Rigidbody.linearVelocity;
//     if (vel.y > 0f) vel.y = Mathf.Min(vel.y, maxAscentSpeed);
//     else            vel.y = Mathf.Max(vel.y, -maxDescentSpeed);
//     Rigidbody.linearVelocity = vel;

//     // Normal mid-air steering while VTOLing
//     LinearMidAirMovement();
//     ForceTurn();
// }



//     private void VTOLMovementold()
//     {
//         // 1) Read inputs
//         float up = Input.GetKey(ascendKey) ? 1f : 0f;
//         float down = Input.GetKey(descendKey) ? 1f : 0f;
//         float vCmd = up - down;  // +1 = ascend, –1 = descend

//         // 2) Apply raw vertical acceleration
//         Rigidbody.AddForce(Vector3.up * vCmd * verticalLiftForce, ForceMode.Acceleration);

//         // 3) Ceiling clamp
//         if (transform.position.y > maxVTOLHeight && up > 0f)
//         {
//             var v = Rigidbody.linearVelocity;
//             if (v.y > 0f) v.y = 0f;
//             Rigidbody.linearVelocity = v;
//         }

//         // 4) Descent control & ground snap
//         if (vCmd < 0f)
//         {
//             // Clamp your downward speed
//             var vel = Rigidbody.linearVelocity;
//             vel.y = Mathf.Max(vel.y, -maxDescendSpeed);
//             Rigidbody.linearVelocity = vel;

//             // Raycast from center down to see your true height
//             if (Physics.Raycast(transform.position, Vector3.down, out var hit, HoverHeight + 1f, GroundLayer))
//             {
//                 float distToGround = hit.distance;
//                 float minAllowed = HoverHeight + groundSnapBuffer;
//                 if (distToGround <= minAllowed)
//                 {
//                     // Snap to just above ground and kill vertical motion
//                     float targetY = hit.point.y + HoverHeight;
//                     transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
//                     vel = Rigidbody.linearVelocity;
//                     vel.y = 0f;
//                     Rigidbody.linearVelocity = vel;
//                     return;  // skip steering this frame
//                 }
//             }
//         }

//         // 5) Allow normal mid-air steering
//         LinearMidAirMovement();
//         ForceTurn();
//     }

//     private void Update()
//     {
//         if (Input.GetKey(KeyCode.LeftShift))      { Pitch = 1; }
//         else if (Input.GetKey(KeyCode.LeftControl)){ Pitch = -1; }
//         else                                      { Pitch = 0; }

//         // VTOL state = any lift/descend input is held
//         bool ascendHeld  = Input.GetKey(ascendKey);
//         bool descendHeld = Input.GetKey(descendKey);
//         isVTOL = ascendHeld || descendHeld;

//         // gravity only when *not* VTOL
//         Rigidbody.useGravity = !isVTOL;

//         if (Input.GetKeyDown(toggleHeadlightsKey))
//             headlightsOn = !headlightsOn;

//         HoverMidAirUpdate();
//     }

//     void FixedUpdate()
//     {
//         if (isVTOL)
//             VTOLMovement();
//         else
//             Controller();

//         // 🔊💡🔥 Update audio + lights + thrusters (does not affect physics)
//         UpdateAudioAndFX();
//     }

//     private void Controller()
//     {
//         LinearMidAirMovement();
//         ForceTurn();

//         if (HoverOnGround)
//             GroundHover();

//         GroundCheck();
//     }

//     /// <summary>
//     /// Mid air controller. All the inputs from the user is taken from here...
//     /// </summary>
//     private void HoverMidAirUpdate()
//     {
//         //vertical input from user.
//         VerticalVal = Input.GetAxis("Vertical");

//         //horizontal input from the user.
//         HorizontalVal = Mathf.Lerp(HorizontalVal, Input.GetAxis("Horizontal"), Time.deltaTime * 2);

//         //the pitch value which controls the pitch of the vehicle.
//         PitchVal = Mathf.Lerp(PitchVal, Pitch, Time.deltaTime * 5);

//         //tilt buttons
//         TiltDownButton = Input.GetKey(TiltDown);
//         TiltUpButton   = Input.GetKey(TiltUp);

//         if (PitchVal < -Threshold)
//             HoverOnGround = false;
//     }

//     /// <summary>
//     /// The vehicle is turned using Torque and the max angle depends on the threshold.
//     /// </summary>
//     private void ForceTurn()
//     {
//         Rigidbody.AddTorque(Vector3.up * RotationSpeed * Time.deltaTime * HorizontalVal, ForceMode.Acceleration);

//         if (!HoverOnGround)
//         {
//             if (Inspector.Z > -MaxSideTiltAngle && Inspector.Z < MaxSideTiltAngle)
//                 Rigidbody.AddTorque(transform.forward * RotationSpeed * Time.deltaTime * -HorizontalVal * SideReturnSpeed, ForceMode.Acceleration);
//         }

//         if (!Input.GetKey(LeftTurn))
//         {
//             if (!HoverOnGround)
//                 if (Inspector.Z > RotationThreshold)
//                     Rigidbody.AddTorque(-transform.forward * RotationSpeed * Time.deltaTime, ForceMode.Acceleration);
//         }

//         if (!Input.GetKey(RightTurn))
//         {
//             if (!HoverOnGround)
//                 if (Inspector.Z < -RotationThreshold)
//                     Rigidbody.AddTorque(transform.forward * RotationSpeed * Time.deltaTime, ForceMode.Acceleration);
//         }

//         if (!HoverOnGround)
//         {
//             if (Inspector.X > -MaxVerticalTiltAngle && Inspector.X < MaxVerticalTiltAngle)
//                 Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * PitchVal / 2, ForceMode.Acceleration);
//         }
//         else
//         {
//             Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * PitchVal / 2, ForceMode.Acceleration);
//         }

//         if (!TiltDownButton && !TiltUpButton)
//         {
//             if (!HoverOnGround)
//             {
//                 if (Inspector.X > RotationThreshold)
//                     Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * -VerticalReturnSpeed, ForceMode.Acceleration);

//                 if (Inspector.X < -RotationThreshold)
//                     Rigidbody.AddTorque(transform.right * RotationSpeed / 2 * Time.deltaTime * VerticalReturnSpeed, ForceMode.Acceleration);
//             }
//         }
//     }

//     /// <summary>
//     /// Controls all the movement from here. Movement is applied using Rigidbody.Velocity..
//     /// </summary>
//     private void LinearMidAirMovement()
//     {
//         if (VerticalVal > Threshold)
//         {
//             Vector3 TargetVelocity = Force * Multiplier * Time.fixedDeltaTime * transform.forward;
//             if (Rigidbody.isKinematic)
//                 Rigidbody.MovePosition(Rigidbody.position + Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f) * Time.fixedDeltaTime);
//             else
//                 Rigidbody.linearVelocity = Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f);
//             MoveTimer = 0;
//         }

//         if (VerticalVal < -Threshold)
//         {
//             Vector3 TargetVelocity = Force * Multiplier * Time.fixedDeltaTime * -transform.forward;
//             if (Rigidbody.isKinematic)
//                 Rigidbody.MovePosition(Rigidbody.position + Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f) * Time.fixedDeltaTime);
//             else
//                 Rigidbody.linearVelocity = Vector3.Lerp(Rigidbody.linearVelocity, TargetVelocity, Time.fixedDeltaTime * VelocityChangeRate / 2f);
//             MoveTimer = 0;
//         }

//         if (VerticalVal == 0f)
//         {
//             MoveTimer += Time.deltaTime / 2.0f;
//             Vector3 lerpedVel = Vector3.Lerp(Rigidbody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * MoveTimer * VelocityChangeRate);
//             if (Rigidbody.isKinematic)
//                 Rigidbody.MovePosition(Rigidbody.position + lerpedVel * Time.fixedDeltaTime);
//             else
//                 Rigidbody.linearVelocity = lerpedVel;
//         }
//     }

//     /// <summary>
//     /// Checks if the ground is in vicinity.
//     /// </summary>
//     private void GroundCheck()
//     {
//         RaycastHit hit;
//         if (Physics.Raycast(transform.position, -Vector3.up, out hit, HoverHeight + GroundDetectRayLength, GroundLayer))
//         {
//             HoverOnGround = true;
//             Rigidbody.useGravity = true;
//         }
//         else
//         {
//             HoverOnGround = false;
//             Rigidbody.useGravity = false;
//         }
//     }

//     /// <summary>
//     /// Hover mechanics of the vehicle when close to the ground.
//     /// </summary>
//     private void GroundHover()
//     {
//         RaycastHit hit;
//         for (int i = 0; i < EnginePoints.Length; i++)
//         {
//             var hoverPoint = EnginePoints[i];
//             if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out hit, HoverHeight, GroundLayer))
//             {
//                 Rigidbody.AddForceAtPosition(Vector3.up * HoverForce * (1.0f - (hit.distance / HoverHeight)), hoverPoint.transform.position);
//             }
//             else
//             {
//                 if (HoverOnGround)
//                 {
//                     if (transform.position.y > hoverPoint.transform.position.y)
//                         Rigidbody.AddForceAtPosition(transform.up * HoverForce, transform.position);
//                     else
//                         Rigidbody.AddForceAtPosition(hoverPoint.transform.up * -HoverForce, hoverPoint.transform.position);
//                 }
//             }
//         }
//     }

//     // ─────────────────────────────────────────────────────────────
//     //                      AUDIO + LIGHTS + VFX
//     // ─────────────────────────────────────────────────────────────
//     void PrepareAudioSource(ref AudioSource src, string goName, AudioClip clip, bool loop, float vol)
//     {
//         if (!clip) return;
//         if (!src)
//         {
//             var go = new GameObject(goName);
//             go.transform.SetParent(transform, false);
//             src = go.AddComponent<AudioSource>();
//             src.playOnAwake = false;
//             src.loop = loop;
//             src.rolloffMode = AudioRolloffMode.Linear;
//         }
//         src.clip = clip;
//         src.volume = vol;
//         src.spatialBlend = debugForce2DAudio ? 0f : 0.7f; // partly 3D but audible
//         src.minDistance = 4f;
//         src.maxDistance = 60f;
//         if (!src.isPlaying) { src.Play(); if (debugLogAudioState) Debug.Log($"[HoverAudio] {goName} start"); }
//     }

//     void UpdateAudioAndFX()
//     {
//         // Inputs/state
//         float throttle = Mathf.Clamp(VerticalVal, -1f, 1f);
//         Vector3 v3D = Rigidbody ? Rigidbody.linearVelocity : Vector3.zero;  // use 3D velocity to estimate speed
//         float fwdSpeed = Vector3.Dot(v3D, transform.forward);
//         float speed01 = Mathf.Clamp01(Mathf.Abs(fwdSpeed) / (Force * Multiplier * Time.fixedDeltaTime + 0.01f));

//         bool ascending = Input.GetKey(ascendKey);
//         bool descending = Input.GetKey(descendKey);

//         // AUDIO
//         if (_srcIdle)  _srcIdle.volume  = engineIdle ? idleVolume : 0f;

//         if (_srcAccel)
//         {
//             float a = Mathf.Max(0f, throttle);
//             _srcAccel.volume = accelVolume * a;
//             _srcAccel.pitch  = Mathf.Lerp(enginePitchRange.x, enginePitchRange.y, Mathf.Max(a, speed01));
//             if (engineAccel && !_srcAccel.isPlaying) _srcAccel.Play();
//         }

//         if (_srcDecel)
//         {
//             float d = Mathf.Max(0f, -throttle);
//             float braking = 0f;
//             if (Mathf.Sign(fwdSpeed) > 0 && throttle < 0) braking = Mathf.Clamp01(-throttle);
//             if (Mathf.Sign(fwdSpeed) < 0 && throttle > 0) braking = Mathf.Clamp01(throttle);

//             float decelMix = Mathf.Clamp01(d + 0.5f * braking);
//             _srcDecel.volume = decelVolume * decelMix;
//             _srcDecel.pitch  = Mathf.Lerp(enginePitchRange.x, enginePitchRange.y, Mathf.Max(decelMix, speed01 * 0.5f));
//             if (engineDecel && !_srcDecel.isPlaying) _srcDecel.Play();
//         }

//         // LIGHTS
//         if (headlights != null)
//         {
//             float hI = headlightsOn ? headlightIntensityOn : headlightIntensityOff;
//             foreach (var l in headlights) if (l) l.intensity = hI;
//         }
//         if (tailLights != null)
//         {
//             bool brakingNow = throttle < -0.05f || descending;
//             float target = brakingNow ? tailLightBrakeIntensity : tailLightIdleIntensity;
//             foreach (var l in tailLights) if (l) l.intensity = Mathf.Lerp(l.intensity, target, 0.25f);
//         }

//         // THRUSTERS
//         float rearRate = Mathf.Lerp(0f, rearThrusterRateMax, Mathf.Clamp01(throttle));
//         SetEmission(rearThrusters, rearRate);

//         float bottomRate = 0f;
//         if (ascending) bottomRate = bottomThrusterRateMax;
//         else if (isVTOL) bottomRate = Mathf.Max(bottomThrusterRateIdle, bottomThrusterRateMax * 0.25f);
//         else if (!HoverOnGround) bottomRate = bottomThrusterRateIdle;
//         else bottomRate = 0f;
//         SetEmission(bottomThrusters, bottomRate);
//     }

//     void SetEmission(ParticleSystem[] systems, float rate)
//     {
//         if (systems == null) return;
//         foreach (var ps in systems)
//         {
//             if (!ps) continue;
//             var em = ps.emission;
//             var r  = em.rateOverTime;
//             r.constant = rate;
//             em.rateOverTime = r;

//             if (rate > 0f) { if (!ps.isPlaying) ps.Play(); }
//             else           { if (ps.isPlaying)  ps.Stop(); }
//         }
//     }
// }
