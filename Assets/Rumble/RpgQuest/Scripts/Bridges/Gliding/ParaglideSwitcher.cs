// Assets/Scripts/Gliding/ParaglideSwitcher.cs
using UnityEngine;

public class ParaglideSwitcher : MonoBehaviour
{
    [Header("Player Side")]
    public GameObject playerRoot;     // whole player (locomotion) root to disable/enable
    public Rigidbody playerRb;        // player rigidbody (if any); optional
    public Camera playerCamera;       // your gameplay camera (to disable during glide)

    [Header("Paraglider")]
    public ParagliderRig paragliderPrefab;
    public Transform spawnFrom;       // usually playerRoot.transform; used for position/rotation
    public KeyCode toggleKey = KeyCode.G;

    [Header("Options")]
    public bool inheritVelocity = true;
    public float forwardKick = 2f;    // small push at launch
    public bool destroyOnLand = true; // else we deactivate & reuse

    ParagliderRig activeRig;
    bool gliding;

    void Reset()
    {
        if (!spawnFrom && playerRoot) spawnFrom = playerRoot.transform;
        if (!playerCamera) playerCamera = Camera.main;
        if (!playerRb && playerRoot) playerRb = playerRoot.GetComponentInChildren<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!gliding) BeginGlide();
            else EndGlide(); // manual exit (you can also call this from ParagliderController on landing)
        }

        if (Input.GetKeyDown(KeyCode.V))
            Debug.Log($" vs={Vector3.Dot(playerRb.linearVelocity, Vector3.up):F1}");
    }
 
bool inTransition;
float glideStartTime;
float landingGraceUntil;

public void SetLandingGrace(float seconds)
{
    landingGraceUntil = Time.time + Mathf.Max(0f, seconds);
}

public void BeginGlide()
{
    if (inTransition) return;
    if (gliding) { Debug.Log("[Switcher] Already gliding; ignoring."); return; }
    if (!paragliderPrefab) { Debug.LogError("[Switcher] No paragliderPrefab assigned."); return; }

    inTransition = true;

    // Spawn
    var pos = spawnFrom ? spawnFrom.position : transform.position;
    var fwd = spawnFrom ? spawnFrom.forward  : transform.forward;

    activeRig = Instantiate(paragliderPrefab, pos, Quaternion.LookRotation(fwd, Vector3.up));
    var rigRB = activeRig.rb;
    
    Debug.Log($"[Switcher] Rig RB? {(rigRB ? "yes" : "no")} v={(rigRB ? rigRB.linearVelocity.magnitude.ToString("F1") : "n/a")}");

// if (activeRig.controller)
// {
//     Debug.Log($"[Switcher] Before BeginGlide: Controller Gliding={activeRig.controller.Gliding}");
//     activeRig.controller.BeginGlide();
//     Debug.Log($"[Switcher] After BeginGlide: Controller Gliding={activeRig.controller.Gliding}");
// }


    // Camera swap FIRST disable player cam, THEN enable glider cam (avoid 2 heavy cams at once)
        SetCameraEnabled(playerCamera, false);
    SetCameraEnabled(activeRig.glideCam, true);

    // Rigidbody bring-up + initial speed
    if (rigRB)
    {
        rigRB.isKinematic = false;
        rigRB.useGravity  = true;
        rigRB.constraints = RigidbodyConstraints.None;
        rigRB.detectCollisions = true;
        rigRB.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rigRB.WakeUp();

        const float minLaunchSpeed = 9f;
        Vector3 v = Vector3.zero;
#if UNITY_6000_0_OR_NEWER
        if (inheritVelocity && playerRb) v = playerRb.linearVelocity;
#else
        if (inheritVelocity && playerRb) v = playerRb.velocity;
#endif
        if (v.magnitude < minLaunchSpeed) v = activeRig.transform.forward * minLaunchSpeed;
#if UNITY_6000_0_OR_NEWER
        rigRB.linearVelocity = v;
#else
        rigRB.velocity = v;
#endif
    }

    // Disable player
    if (playerRoot) playerRoot.SetActive(false);

    // Tell controller to begin + give it a short landing grace
    // if (activeRig.controller)
    // {
    //     // Optional: provide a 1s landing-grace to avoid instant EndGlide on spawn
    //     activeRig.controller.BeginGlide();
    //     activeRig.controller.SetLandingGrace(1.0f); // add method below to controller
    //     Debug.Log($"[Switcher] Called controller.BeginGlide(). Controller Gliding? {activeRig.controller.Gliding}");
    // }
    // else
    // {
    //     Debug.LogWarning("[Switcher] No ParagliderController on rig.");
    // }

    gliding = true;
    glideStartTime = Time.time;
    inTransition = false;
}


    public void EndGlide()
    {
        if (!gliding) return;

        // Reposition player at rig location
        Vector3 pos = activeRig ? activeRig.transform.position : (spawnFrom ? spawnFrom.position : transform.position);
        Quaternion rot = activeRig ? activeRig.transform.rotation : (spawnFrom ? spawnFrom.rotation : transform.rotation);

        if (playerRoot)
        {
            playerRoot.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rot.eulerAngles.y, 0f));
            playerRoot.SetActive(true);
        }

        // Transfer velocity back (optional)
        if (playerRb && activeRig && activeRig.rb)
        {
#if UNITY_6000_0_OR_NEWER
            playerRb.linearVelocity = activeRig.rb.linearVelocity;
#else
            playerRb.velocity = activeRig.rb.velocity;
#endif
        }

        // Camera: swap back
        SetCameraEnabled(activeRig ? activeRig.glideCam : null, false);
        SetCameraEnabled(playerCamera, true);

        // Remove or hide rig
        if (activeRig)
        {
            if (destroyOnLand) Destroy(activeRig.gameObject);
            else activeRig.gameObject.SetActive(false);
            activeRig = null;
        }

        gliding = false;
    }

    void SetCameraEnabled(Camera cam, bool on)
    {
        if (!cam) return;
        cam.enabled = on;
        var listener = cam.GetComponent<AudioListener>();
        if (listener) listener.enabled = on;
    }
}
