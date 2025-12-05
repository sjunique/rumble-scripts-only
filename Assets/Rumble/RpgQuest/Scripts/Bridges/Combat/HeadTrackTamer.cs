using UnityEngine;
using Invector.vCharacterController;
using Invector;
[DefaultExecutionOrder(-20)]
public class HeadTrackTamer : MonoBehaviour
{
    public float targetDistance = 100f;
    [Range(-1f,1f)] public float facingThreshold = -0.15f; // facing camera if dot < threshold
    public bool disableWhileAiming = true;

    vShooterMeleeInput shooter;
    Component headTrack;     // vHeadTrack (kept generic for version safety)
    Transform farTarget;
    Camera cam;

    void Awake()
    {
        shooter = GetComponent<vShooterMeleeInput>();
        foreach (var c in GetComponentsInChildren<Component>(true))
            if (c.GetType().Name.Contains("vHeadTrack")) { headTrack = c; break; }

        var t = new GameObject("HeadTrackFarTarget").transform;
        t.hideFlags = HideFlags.DontSave;
        farTarget = t;
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!headTrack) return;
        if (!cam) cam = Camera.main;
        if (!cam) return;

        // keep the look point far in front of camera
        farTarget.position = cam.transform.position + cam.transform.forward * targetDistance;

        // try to set 'target' or 'lookTarget' if the field exists
        var tField = headTrack.GetType().GetField("target") ?? headTrack.GetType().GetField("lookTarget");
        if (tField != null) tField.SetValue(headTrack, farTarget);

        // facing-camera detection (character forward vs camera forward)
        bool facingCamera = Vector3.Dot(transform.forward, cam.transform.forward) < facingThreshold;

        // read isAiming if available
        bool isAiming = false;
        if (shooter)
        {
            var p = shooter.GetType().GetProperty("isAiming");
            if (p != null) isAiming = (bool)p.GetValue(shooter, null);
        }

        bool suppress = facingCamera || (disableWhileAiming && isAiming);

        // enable/disable the whole head-track GameObject for stability
        headTrack.gameObject.SetActive(!suppress);
    }
}
