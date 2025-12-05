using System;
using UnityEngine;
using Invector.vCharacterController;
using System.Collections;

[DefaultExecutionOrder(10000)]
public class WaterCarSummon : MonoBehaviour
{
    // ── Events ───────────────────────────────────────────────────────────────────
    /// <summary>Fired after car is moved/activated at new position.</summary>
    public event Action<GameObject, Vector3> OnCarSummoned;
    /// <summary>Optional: fire when you hide/disable the car intentionally.</summary>
    public event Action OnCarDismissed;

    // ── Refs ─────────────────────────────────────────────────────────────────────
    [Header("Refs")]
    public vThirdPersonController player;      // scene player
    public GameObject carRoot;                 // car root object
    public string summonSpawnKey = "OceanDock";// used only for DockByKey
    public SummonMode defaultMode = SummonMode.NearPlayer;
    public KeyCode summonKey = KeyCode.H;

    [Header("Near Player Spawn")]
    public Transform referenceFrame;           // usually player or camera; null -> player
    public float spawnDistance = 4f;
    public float heightAboveGround = 1.2f;
    public LayerMask groundLayers = ~0;
    public float groundProbeUp = 10f;
    public float groundProbeDown = 40f;

    [Header("Offsets & Separation")]
    public float sideOffset = 1.8f;
    public bool spawnOnRightSide = true;
    public float minPlayerSeparation = 2.5f;

    public enum SummonMode { DockByKey, NearPlayer }

    [Header("Post-summon hold")]
    public float stickAfterSummonSeconds = 0.8f;
    float _holdUntil;
    Vector3 _heldPos;
    Quaternion _heldRot;
    Coroutine _restoreKinCo;

    [Header("Clearance Check")]
    public float clearanceRadius = 1.5f;
    public float clearanceHeight = 2.0f;
    public int clearanceSamples = 12;

    [Header("Visuals")]
    public bool hideDuringTeleport = true;

    [Header("Legacy Water Check (unused by default)")]
    public LayerMask waterLayers;
    public float probeHeight = 1.2f;
    public float probeRadius = 0.4f;

    private GameLocationManager loc;

    // ── Linker hook ──────────────────────────────────────────────────────────────
    public void AssignLinked(vThirdPersonController p, GameObject car)
    {
        if (p)   player = p;
        if (car) carRoot = car;
    }

    // ── Unity Lifecycle ─────────────────────────────────────────────────────────
    void Awake()
    {
        // Pull from Linker if available
        var link = PlayerCarLinker.Instance;
        if (link)
        {
            if (!player)  player  = link.player;
            if (!carRoot) carRoot = link.carRoot;
        }
        EnsureLoc();
        if (!referenceFrame) referenceFrame = player ? player.transform : null;
    }

    void LateUpdate()
    {
        if (carRoot && Time.time < _holdUntil)
        {
            var t = carRoot.transform;
            t.SetPositionAndRotation(_heldPos, _heldRot);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(summonKey))
            SummonNow();
    }

    // ── Public API ───────────────────────────────────────────────────────────────
    /// <summary>Programmatic summon. Defaults to NearPlayer.</summary>
    public bool SummonNow(SummonMode? mode = null, string overrideSpawnKey = null)
    {
        Debug.Log("carrood"+carRoot);

        if (!carRoot) { Diag.Error("SUMMON", "No carRoot assigned.", this); return false; }
        var m = mode ?? defaultMode;

        if (m == SummonMode.NearPlayer)
            return SummonNearPlayer(referenceFrame ? referenceFrame : (player ? player.transform : null));

        EnsureLoc();
        string key = string.IsNullOrWhiteSpace(overrideSpawnKey) ? summonSpawnKey : overrideSpawnKey;
        if (string.IsNullOrWhiteSpace(key)) { Diag.Error("SUMMON", "summonSpawnKey empty.", this); return false; }

        return TeleportToSpawnKey(key);
    }

    /// <summary>Warp car to a safe spot near the player/camera, aligned to ground.</summary>
    public bool SummonNearPlayer(Transform reference)
    {
        if (!carRoot) { Diag.Error("SUMMON", "No carRoot assigned.", this); return false; }
        if (!reference) reference = player ? player.transform : transform;

        Vector3 fwd = reference.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = reference.right;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        Vector3 lateral = right * (spawnOnRightSide ? sideOffset : -sideOffset);

        Vector3 probeStart = reference.position + fwd * spawnDistance + lateral + Vector3.up * groundProbeUp;

        bool hit; RaycastDown(probeStart, groundProbeUp + groundProbeDown, out hit);
        Vector3 pos = hit
            ? lastHitPoint + Vector3.up * heightAboveGround
            : reference.position + fwd * spawnDistance + lateral + Vector3.up * heightAboveGround;

        float min = Mathf.Max(0.1f, minPlayerSeparation);
        if (player)
        {
            Vector3 playerXZ = new Vector3(player.transform.position.x, pos.y, player.transform.position.z);
            Vector3 d = pos - playerXZ; d.y = 0f;
            float dist = d.magnitude;
            if (dist < min)
            {
                Vector3 pushDir = dist > 1e-4f ? (d / dist) : (spawnOnRightSide ? right : -right);
                pos = playerXZ + pushDir * min;
            }
        }

        if (IsBlocked(pos))
        {
            for (int i = 1; i <= Mathf.Max(1, clearanceSamples); i++)
            {
                float ang = (360f / clearanceSamples) * i;
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * fwd;
                Vector3 start = reference.position + dir * spawnDistance + lateral + Vector3.up * groundProbeUp;
                if (RaycastDown(start, groundProbeUp + groundProbeDown, out bool cHit))
                {
                    Vector3 cand = lastHitPoint + Vector3.up * heightAboveGround;
                    if (player)
                    {
                        Vector3 pXZ = new Vector3(player.transform.position.x, cand.y, player.transform.position.z);
                        Vector3 dd = cand - pXZ; dd.y = 0f;
                        float dist2 = dd.magnitude;
                        if (dist2 < min) cand = pXZ + (dist2 > 1e-4f ? dd.normalized : dir) * min;
                    }
                    if (!IsBlocked(cand)) { pos = cand; break; }
                }
            }
        }

        Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);
        WarpCar(pos, rot);
        Diag.Info("SUMMON", $"Car summoned near player at {pos}", this);
        return true;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    Vector3 lastHitPoint;

    bool RaycastDown(Vector3 start, float dist, out bool hit)
    {
        if (Physics.Raycast(start, Vector3.down, out var rh, dist, groundLayers, QueryTriggerInteraction.Ignore))
        {
            lastHitPoint = rh.point;
            hit = true; return true;
        }
        hit = false; return false;
    }

    bool IsBlocked(Vector3 pos)
    {
        Vector3 half = new Vector3(clearanceRadius, clearanceHeight * 0.5f, clearanceRadius);
        return Physics.CheckBox(pos + Vector3.up * (clearanceHeight * 0.5f), half, Quaternion.identity,
                                ~0, QueryTriggerInteraction.Ignore);
    }

    void WarpCar(Vector3 pos, Quaternion rot)
    {
        CarSummonGuard.BeginBlock(0.9f);

        bool wasActive = carRoot.activeSelf;
        if (hideDuringTeleport && wasActive) SetRenderers(carRoot, false);
        if (!wasActive) carRoot.SetActive(true);

        var rb = carRoot.GetComponentInChildren<Rigidbody>();
        bool hadRb = rb != null;
        bool prevKin = false;

        if (hadRb)
        {
            prevKin = rb.isKinematic;
            rb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
#endif
            rb.Sleep();
        }

        carRoot.transform.SetPositionAndRotation(pos, rot);

        if (hideDuringTeleport) SetRenderers(carRoot, true);

        _heldPos = pos; _heldRot = rot;
        _holdUntil = Time.time + stickAfterSummonSeconds;

        if (hadRb)
        {
            if (_restoreKinCo != null) StopCoroutine(_restoreKinCo);
            _restoreKinCo = StartCoroutine(RestoreKinematic(rb, prevKin, stickAfterSummonSeconds));
        }

        // 🔔 Fire event AFTER pose is finalized
        OnCarSummoned?.Invoke(carRoot, pos);
    }

    IEnumerator RestoreKinematic(Rigidbody rb, bool prevKin, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb) rb.isKinematic = prevKin;
    }

    bool TeleportToSpawnKey(string key)
    {
        if (Time.time < _holdUntil) { Diag.Info("SUMMON", "Dock teleport suppressed (holding NearPlayer summon).", this); return true; }
        if (CarSummonGuard.IsBlocked && carRoot.CompareTag("Vehicle")) return false;

        EnsureLoc();
        if (!loc) { Diag.Warn("SUMMON", "No GameLocationManager; cannot use spawn keys.", this); return false; }
        bool wasActive = carRoot.activeSelf;
        if (hideDuringTeleport && wasActive) SetRenderers(carRoot, false);
        bool ok = TeleportService.TeleportTransformToSpawn(carRoot.transform, key, loc);
        if (!wasActive) carRoot.SetActive(true);
        if (hideDuringTeleport) SetRenderers(carRoot, true);
        if (ok)
        {
            Diag.Info("SUMMON", $"Car summoned to '{key}'.", this);
            OnCarSummoned?.Invoke(carRoot, carRoot.transform.position);
        }
        else Diag.Warn("SUMMON", $"Teleport to '{key}' failed.", this);
        return ok;
    }

    void SetRenderers(GameObject root, bool visible)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++) if (rends[i]) rends[i].enabled = visible;
    }

    void EnsureLoc()
    {
        if (!loc) loc = FindObjectOfType<GameLocationManager>();
        if (!player) player = FindObjectOfType<vThirdPersonController>(true);
    }

    // Legacy (kept)
    bool IsPlayerInWater()
    {
        if (!player) return false;
        var pos = player.transform.position + Vector3.up * (probeHeight * 0.5f);
        var half = new Vector3(probeRadius, probeHeight * 0.5f, probeRadius);
        return Physics.CheckBox(pos, half, Quaternion.identity, waterLayers, QueryTriggerInteraction.Collide);
    }
}
