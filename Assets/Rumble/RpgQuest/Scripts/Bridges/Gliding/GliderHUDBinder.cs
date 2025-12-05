using UnityEngine;
using UnityEngine.UIElements;

public class GliderHUDBinder : MonoBehaviour
{
    [Header("Refs")]
    public UIDocument ui;
    public ParagliderController controller;   // assign (from your paraglider prefab)
    public Rigidbody rb;                      // controller.rb, assigned for convenience

    [Header("Display")]
    public int speedDecimals = 1;
    public float varioMin = -6f;              // m/s sink
    public float varioMax =  +6f;             // m/s climb
    public float varioSmoothing = 6f;         // bigger = smoother
    public bool showOnlyWhenGliding = true;

    // UI
    Label airL, groundL, aglL, vsL, trimL;
    ProgressBar varioBar;
    VisualElement root;

    // runtime
    float smoothedVario;
    float lastAGL;
    Vector3 lastVel;

    void Awake()
    {
        if (!ui) ui = GetComponent<UIDocument>();
        root     = ui.rootVisualElement;
        airL     = root.Q<Label>("airspeed");
        groundL  = root.Q<Label>("groundspeed");
        aglL     = root.Q<Label>("agl");
        vsL      = root.Q<Label>("vs");
        trimL    = root.Q<Label>("trim");
        varioBar = root.Q<ProgressBar>("varioBar");

        if (!controller) controller = FindAnyObjectByType<ParagliderController>();
        if (!rb && controller) rb = controller.rb;
    }

    void Update()
    {
        if (!controller || !rb) { root.style.display = DisplayStyle.None; return; }

        // Show/Hide with glide state
        if (showOnlyWhenGliding)
            root.style.display = controller.Gliding ? DisplayStyle.Flex : DisplayStyle.None;

        if (!controller.Gliding) return;

        // AIRSPEED = speed relative to air (wind)
        Vector3 airVel = rb.linearVelocity - controller.constantWind;  // add thermals if you use them
        float airSpeed = airVel.magnitude;

        // GROUNDSPEED = world velocity magnitude
        float groundSpeed = rb.linearVelocity.magnitude;

        // AGL: reuse the controller’s raycast parameters
        float agl = SampleAGL(controller);

        // Vario (vertical speed), smoothed
        float rawVario = Vector3.Dot(rb.linearVelocity, Vector3.up);
        smoothedVario = Mathf.Lerp(smoothedVario, rawVario, 1f - Mathf.Exp(-varioSmoothing * Time.deltaTime));

        // Trim (map -1..+1 → 0..100%) — we don’t expose trim directly; infer from input if you want.
        // If you made 'trim' public in your controller, read it directly. Here we approximate using pitch axis:
        float approxTrim01 = Mathf.InverseLerp(-1f, 1f, Input.GetAxis(controller.pitchAxis));
        int trimPct = Mathf.RoundToInt(Mathf.Lerp(0f, 100f, approxTrim01));

        // Update UI
        airL.text    = $"Air: {airSpeed.ToString($"F{speedDecimals}")} m/s";
        groundL.text = $"Ground: {groundSpeed.ToString($"F{speedDecimals}")} m/s";
        aglL.text    = $"AGL: {agl.ToString("F1")} m";
        vsL.text     = $"Vario: {(smoothedVario>=0?"+":"")}{smoothedVario.ToString("F1")} m/s";
        trimL.text   = $"Trim: {trimPct}%";

        // Vario bar (0..100, 50 = level)
        float t = Mathf.InverseLerp(varioMin, varioMax, smoothedVario);
        varioBar.value = Mathf.Clamp01(t) * 100f;

        // Optional: tint bar (green climb / red sink)
        Color c = smoothedVario >= 0 ? new Color(0.25f, 1f, 0.25f, 0.9f) : new Color(1f, 0.35f, 0.35f, 0.9f);
        varioBar.style.unityBackgroundImageTintColor = new StyleColor(c);

        lastAGL = agl;
        lastVel = rb.linearVelocity;
    }

    float SampleAGL(ParagliderController pc)
    {
        var origin = pc ? (pc.groundProbe ? pc.groundProbe.position : pc.transform.position) : transform.position;
        if (Physics.Raycast(origin, Vector3.down, out var hit, pc.probeDistance, pc.groundMask, QueryTriggerInteraction.Ignore))
            return hit.distance;
        return lastAGL; // keep last good value if nothing hit
    }
}

