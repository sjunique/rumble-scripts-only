using UnityEngine;
using System;
using System.Reflection;

public class BeltLaserShooter_v3 : MonoBehaviour
{
    [Header("Input")]
    public KeyCode fireKey = KeyCode.Mouse0;   // LMB by default
    public bool alsoUseMouseButton = true;     // fire on Input.GetMouseButtonDown(0)
    public KeyCode aimKey  = KeyCode.Mouse1;   // RMB to “aim” (optional)
    public float fireRate = 6f;                // shots/second

    [Header("Refs")]
    public Transform aimPivot;                 // rotates to face aim
    public Transform muzzle;                   // forward (+Z) = shot dir
    public ParticleSystem ringLaser;           // your ring particle
    public GameObject muzzleVfx;               // optional
    public float vfxLife = 1.0f;

    [Header("Shooting")]
    public float range = 80f;
    public LayerMask hitMask = ~0;             // MUST include AI layer

    [Header("AI reaction")]
    public float knockback = 12f;
    public float fleeSeconds = 2.5f;
    public float initialKick = 0.8f;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool drawShotLine = true;
    public float shotLineDuration = 0.08f;
    public Color shotLineColor = Color.cyan;

    float _nextFireTime;

    void Update()
    {
        var cam = GetActiveCamera();
        if (!cam)
        {
            if (debugLogs) Debug.LogWarning("[BeltLaser] No camera found (no Cinemachine Brain and no Camera.main).");
            return;
        }

        // Aim pivot tracks the camera forward
        if (aimPivot)
        {
            Vector3 aimPoint = cam.position + cam.forward * 50f;
            Vector3 dir = (aimPoint - aimPivot.position);
            if (dir.sqrMagnitude > 1e-4f)
                aimPivot.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // Fire input
        bool firePressed = Input.GetKeyDown(fireKey) || (alsoUseMouseButton && Input.GetMouseButtonDown(0));
        if (Time.time >= _nextFireTime && firePressed)
        {
            _nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
            FireOnce();
        }
    }

    void FireOnce()
    {
        if (!muzzle)
        {
            if (debugLogs) Debug.LogError("[BeltLaser] Muzzle is NOT assigned.");
            return;
        }

        if (muzzleVfx)
            Destroy(Instantiate(muzzleVfx, muzzle.position, muzzle.rotation), vfxLife);

        if (ringLaser)
        {
            ringLaser.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ringLaser.Play(true);
        }
        else if (debugLogs) Debug.LogWarning("[BeltLaser] ringLaser not assigned (particles won’t show).");

        Vector3 origin = muzzle.position;
        Vector3 dir = muzzle.forward; // MUST be +Z of the muzzle

        if (drawShotLine)
            Debug.DrawLine(origin, origin + dir * range, shotLineColor, shotLineDuration);

        if (Physics.Raycast(origin, dir, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (debugLogs)
                Debug.Log($"[BeltLaser] Hit {hit.collider.name} @ {hit.point} (layer={LayerMask.LayerToName(hit.collider.gameObject.layer)})");

            // Knockback if rigidbody
            var rb = hit.rigidbody;
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.AddForce(dir * knockback, ForceMode.VelocityChange);
            }

            // Emerald flee (if adapter is present on the AI)
            var flee = hit.collider.GetComponentInParent<EmeraldFleeAdapter>();
            if (flee)
            {
                Vector3 away = (hit.collider.transform.position - origin).normalized;
                flee.FleeFrom(away, fleeSeconds, initialKick);
            }
        }
        else
        {
            if (debugLogs) Debug.Log("[BeltLaser] No hit (check hitMask and muzzle forward).");
        }
    }

    // -------- Camera resolver: supports Cinemachine 2.x and 3.x via reflection --------
    Transform GetActiveCamera()
    {
        // 1) Try Cinemachine Brain (CM3 namespace first)
        var brain = FindCinemachineBrainTransform();
        if (brain != null)
        {
            // Brain has property OutputCamera (CM2 & CM3)
            var outCam = GetProp(brain, "OutputCamera") as Camera;
            if (outCam) return outCam.transform;

            // Fallback: brain is on the MainCamera; return that
            var comp = brain as Component;
            if (comp && comp.GetComponent<Camera>()) return comp.GetComponent<Camera>().transform;
        }

        // 2) Fallback to Camera.main
        if (Camera.main) return Camera.main.transform;

        // 3) Last resort: any enabled camera
        var cams = FindObjectsOfType<Camera>();
        foreach (var c in cams) if (c.enabled) return c.transform;

        return null;
    }

    UnityEngine.Object FindCinemachineBrainTransform()
    {
        // Try Unity.Cinemachine.CinemachineBrain (CM3)
        var t = ResolveType("Unity.Cinemachine.CinemachineBrain", "Cinemachine.CinemachineBrain");
        if (t != null)
        {
            var obj = FindObjectOfType(t) as UnityEngine.Object;
            return obj;
        }
        return null;
    }

    static Type ResolveType(params string[] names)
    {
        foreach (var n in names)
        {
            var t = Type.GetType(n);
            if (t != null) return t;
        }
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var n in names)
            {
                var t = asm.GetType(n);
                if (t != null) return t;
            }
        }
        return null;
    }

    static object GetProp(object obj, string name)
    {
        if (obj == null) return null;
        var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (p == null) return null;
        try { return p.GetValue(obj); } catch { return null; }
    }
}
