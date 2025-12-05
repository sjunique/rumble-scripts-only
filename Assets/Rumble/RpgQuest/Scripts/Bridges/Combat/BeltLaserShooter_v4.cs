using UnityEngine;
using System;
using System.Reflection;

public class BeltLaserShooter_v4 : MonoBehaviour
{
    [Header("Aim Assist")]
    public float maxAimDistance = 120f;
    [Range(0f, 30f)] public float screenAimRadiusPx = 80f;  // cone around center
    [Range(0f, 30f)] public float worldAimConeDeg = 8f;     // angular cone

    [Header("Layers")]
    public string playerLayerName = "Default";
    public LayerMask aiMask;                   // generic AI layers (no Emerald dependency)
    public LayerMask worldMask = ~0;           // terrain/props
    public LayerMask hitMask = ~0;             // final shot mask (exclude player)

    [Header("Beam (Game view visible)")]
    public LineRenderer beam;                  // optional; assign a LineRenderer
    public float beamTime = 0.05f;

    [Header("Input")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public bool alsoUseMouseButton = true;
    public float fireRate = 6f;

    [Header("Refs")]
    public Transform aimPivot;                 // rotates toward aim point
    public Transform muzzle;                   // origin of shot
    public ParticleSystem ringLaser;           // ring particle
    public GameObject muzzleVfx;               // optional
    public float vfxLife = 1.0f;

    [Header("Raycasting")]
    public float range = 100f;
    public LayerMask aimMask = ~0;             // camera ray "what can we aim at"

    [Header("Particle Orientation Fix")]
    [Tooltip("Extra local rotation to make the ring face along the shot. Example: (-90,0,0).")]
    public Vector3 ringExtraEuler = new Vector3(-90, 0, 0);

    [Header("Physics Reaction (generic)")]
    public float knockback = 12f;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool drawLines = true;
    public Color aimLineColor = Color.yellow;
    public Color shotLineColor = Color.cyan;
    public float lineTime = 0.08f;

    float _nextFire;

    void Awake()
    {
        // Ensure beam reference is a scene instance
        if (beam && !beam.gameObject.scene.IsValid())
        {
            Debug.LogWarning("[BeltLaser] Beam reference points to a prefab. Drag the SCENE instance into the slot.");
            beam = null;
        }
        if (beam)
        {
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            if (beam.widthMultiplier <= 0f) beam.widthMultiplier = 0.03f;
        }
    }

    void Update()
    {
        var cam = GetActiveCamera();
        if (!cam) { if (debugLogs) Debug.LogWarning("[BeltLaser] No active camera."); return; }

        // 1) Choose targetPoint from the camera, preferring AI first
        Vector3 aimOrigin = cam.position;
        Vector3 aimDir = cam.forward;
        Vector3 targetPoint = aimOrigin + aimDir * range;

        if (aiMask != 0 && Physics.Raycast(aimOrigin, aimDir, out var aiHit, range, aiMask, QueryTriggerInteraction.Ignore))
            targetPoint = aiHit.point;
        else if (Physics.Raycast(aimOrigin, aimDir, out var anyHit, range, aimMask, QueryTriggerInteraction.Ignore))
            targetPoint = anyHit.point;

        if (drawLines) Debug.DrawLine(aimOrigin, targetPoint, aimLineColor, lineTime);

        // 2) Shoot from muzzle → targetPoint using a SphereCast
        if (!muzzle) return;
        Vector3 origin = muzzle.position;
        Vector3 shotDir = (targetPoint - origin).sqrMagnitude > 1e-6f ? (targetPoint - origin).normalized : muzzle.forward;
        float radius = 1.0f;
        if (drawLines) Debug.DrawLine(origin, origin + shotDir * range, shotLineColor, lineTime);

        if (Physics.SphereCast(origin, radius, shotDir, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (debugLogs)
                Debug.Log($"[BeltLaser] Hit {hit.collider.name} (layer={LayerMask.LayerToName(hit.collider.gameObject.layer)})");
        }

        // 3) Rotate AimPivot to face target
        if (aimPivot)
        {
            Vector3 dir = targetPoint - aimPivot.position;
            if (dir.sqrMagnitude > 1e-4f)
                aimPivot.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // 4) Fire input
        bool firePressed = Input.GetKeyDown(fireKey) || (alsoUseMouseButton && Input.GetMouseButtonDown(0));
        if (Time.time >= _nextFire && firePressed)
        {
            _nextFire = Time.time + (1f / Mathf.Max(0.01f, fireRate));
            FireOnce(targetPoint);
        }
    }

    void FireOnce(Vector3 fallbackTargetPoint)
    {
        if (!muzzle)
        {
            if (debugLogs) Debug.LogError("[BeltLaser] Muzzle not assigned.");
            return;
        }

        // 0) Resolve camera
        var camT = GetActiveCamera(); if (!camT) return;
        var cam = camT.GetComponent<Camera>();
        if (!cam) cam = Camera.main;

        // 1) Choose best target point (auto-aim to generic AI first)
        Vector3 targetPoint = FindAutoAimTargetPoint(cam)
                              ?? CamRayPreferAI(cam)
                              ?? fallbackTargetPoint;

        // 2) Build shot from muzzle -> targetPoint
        Vector3 origin = muzzle.position;
        Vector3 dir = (targetPoint - origin);
        if (dir.sqrMagnitude < 1e-6f) dir = muzzle.forward; else dir.Normalize();

        Vector3 endPoint = origin + muzzle.forward * 60f;

        // Beam
        if (beam)
        {
            float len = Vector3.Distance(origin, endPoint);
            if (debugLogs) Debug.Log($"[BeltLaser] Beam len={len:F2} from {origin} -> {endPoint}");
            beam.enabled = true;
            beam.positionCount = 2;
            beam.useWorldSpace = true;
            beam.SetPosition(0, origin);
            beam.SetPosition(1, endPoint);
            CancelInvoke(nameof(HideBeam));
            Invoke(nameof(HideBeam), beamTime);
        }

        if (drawLines) Debug.DrawLine(origin, origin + dir * range, shotLineColor, lineTime);

        // VFX
        if (muzzleVfx) Destroy(Instantiate(muzzleVfx, origin, Quaternion.LookRotation(dir)), vfxLife);
        if (ringLaser)
        {
            ringLaser.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir) * Quaternion.Euler(ringExtraEuler));
            ringLaser.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ringLaser.Play(true);
        }

        // 3) CapsuleCastAll -> prefer AI, else first hit
        float capsuleRadius = 0.8f;
        float capsuleHeight = 1.2f;
        Vector3 p1 = origin + Vector3.up * (capsuleHeight * 0.25f);
        Vector3 p2 = origin - Vector3.up * (capsuleHeight * 0.75f);

        int playerLayer = LayerMask.NameToLayer(playerLayerName);
        int shotMask = hitMask;
        if (playerLayer >= 0) shotMask &= ~(1 << playerLayer);

        var hits = Physics.CapsuleCastAll(p1, p2, capsuleRadius, dir, range, shotMask, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit? best = null;
        foreach (var h in hits)
            if (((1 << h.collider.gameObject.layer) & aiMask) != 0) { best = h; break; }
        if (best == null && hits.Length > 0) best = hits[0];

        if (best.HasValue)
        {
            var hit = best.Value;
            if (debugLogs) Debug.Log($"[BeltLaser] Hit {hit.collider.name} (layer={LayerMask.LayerToName(hit.collider.gameObject.layer)})");

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
            // (Emerald AI flee/tame logic removed)
        }
        else
        {
            if (debugLogs) Debug.Log("[BeltLaser] CapsuleCastAll missed.");
        }
    }

    void HideBeam()
    {
        if (beam) beam.enabled = false;
    }

    // ------------- Camera resolver (Cinemachine-friendly) -------------
    Transform GetActiveCamera()
    {
        var brain = FindBrain();
        if (brain != null)
        {
            var outputCam = GetProp(brain, "OutputCamera") as Camera;
            if (outputCam) return outputCam.transform;

            var comp = brain as Component;
            var cam = comp ? comp.GetComponent<Camera>() : null;
            if (cam) return cam.transform;
        }

        if (Camera.main) return Camera.main.transform;

        var cams = FindObjectsOfType<Camera>();
        foreach (var c in cams) if (c.enabled) return c.transform;

        return null;
    }

    UnityEngine.Object FindBrain()
    {
        var t = ResolveType("Unity.Cinemachine.CinemachineBrain", "Cinemachine.CinemachineBrain");
        if (t != null) return FindObjectOfType(t) as UnityEngine.Object;
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

    // --------- Auto-aim (generic AI layer) ----------
    Vector3? FindAutoAimTargetPoint(Camera cam)
    {
        var colliders = Physics.OverlapSphere(transform.position, maxAimDistance, aiMask, QueryTriggerInteraction.Ignore);
        if (colliders == null || colliders.Length == 0) return null;

        Vector2 screenCenter = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
        float bestScore = float.MaxValue;
        Vector3? bestPoint = null;

        foreach (var c in colliders)
        {
            if (!c || !c.gameObject.activeInHierarchy) continue;

            var b = c.bounds;
            var pos = b.center + Vector3.up * Mathf.Min(b.extents.y * 0.5f, 0.4f);

            Vector3 to = pos - cam.transform.position;
            if (Vector3.Dot(cam.transform.forward, to.normalized) < Mathf.Cos(worldAimConeDeg * Mathf.Deg2Rad)) continue;

            var sp = cam.WorldToScreenPoint(pos);
            if (sp.z <= 0f) continue;
            float distPx = Vector2.Distance(new Vector2(sp.x, sp.y), screenCenter);
            if (distPx > screenAimRadiusPx) continue;

            float score = distPx + to.magnitude * 0.05f;
            if (score < bestScore)
            {
                bestScore = score;
                bestPoint = pos;
            }
        }
        return bestPoint;
    }

    // Camera ray that prefers AI first, else world
    Vector3? CamRayPreferAI(Camera cam)
    {
        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (aiMask != 0 && Physics.Raycast(origin, dir, out var aiHit, range, aiMask, QueryTriggerInteraction.Ignore))
            return aiHit.point;

        if (Physics.Raycast(origin, dir, out var anyHit, range, worldMask, QueryTriggerInteraction.Ignore))
            return anyHit.point;

        return null;
    }
}
