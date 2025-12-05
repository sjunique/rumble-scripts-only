// Assets/Rumble/System/PlayerRespawnManager.cs
using UnityEngine;
 
using System.Reflection;
using Invector.vCharacterController;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance { get; private set; }

    [Header("Respawn Points")]
    public Transform defaultSpawn;
    public string checkpointTag = "RespawnPoint";

    // runtime
    private Transform _current;
    private bool _hasOverride;
    private Vector3 _overridePos;
    private Quaternion _overrideRot;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!_current) _current = defaultSpawn;
        Debug.Log($"[Respawn] Manager alive. Default={defaultSpawn?.name ?? "NULL"} Tag={checkpointTag}");
    }

    public void SetCheckpoint(Transform t)
    {
        if (!t) return;
        _current = t;
        Debug.Log($"[Respawn] Set checkpoint → {t.name}");
    }

    public void SetOneShotDeathPoint(Vector3 pos, Quaternion rot)
    {
        _hasOverride = true;
        _overridePos = pos;
        _overrideRot = rot;
        Debug.Log("[Respawn] One-shot death point set.");
    }

    public void RespawnPlayer(Transform player)
    {
        if (!player) { Debug.LogWarning("[Respawn] No player transform."); return; }

        // choose spawn
        Vector3 pos;
        Quaternion rot;
        if (_hasOverride) { pos = _overridePos; rot = _overrideRot; _hasOverride = false; }
        else if (_current) { pos = _current.position; rot = _current.rotation; }
        else if (defaultSpawn) { pos = defaultSpawn.position; rot = defaultSpawn.rotation; }
        else { Debug.LogWarning("[Respawn] No spawn set; aborting."); return; }

        // ---- Invector-safe teleport ----
        var tpsInput = player.GetComponentInChildren<vThirdPersonInput>(true);
        if (tpsInput != null)
        {
            var mi = tpsInput.GetType().GetMethod("SetLockInput", BindingFlags.Public | BindingFlags.Instance);
            if (mi != null) mi.Invoke(tpsInput, new object[] { true });
            else
            {
                var f = tpsInput.GetType().GetField("lockInput", BindingFlags.Public | BindingFlags.Instance);
                if (f != null) f.SetValue(tpsInput, true);
            }
        }

        var anim    = player.GetComponentInChildren<Animator>(true);
        var capsule = player.GetComponentInChildren<CapsuleCollider>(true);
        var rb      = player.GetComponentInChildren<Rigidbody>(true);

        if (anim)    anim.applyRootMotion = false;
        bool reEnableCapsule = capsule && capsule.enabled;
        if (capsule) capsule.enabled = false;
        if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.Sleep(); }

        player.SetPositionAndRotation(pos, rot);
        Physics.SyncTransforms();

        if (reEnableCapsule) capsule.enabled = true;
        if (anim) { anim.Rebind(); anim.Update(0f); anim.applyRootMotion = true; }

        if (tpsInput != null)
        {
            var mi = tpsInput.GetType().GetMethod("SetLockInput", BindingFlags.Public | BindingFlags.Instance);
            if (mi != null) mi.Invoke(tpsInput, new object[] { false });
            else
            {
                var f = tpsInput.GetType().GetField("lockInput", BindingFlags.Public | BindingFlags.Instance);
                if (f != null) f.SetValue(tpsInput, false);
            }
        }

        Debug.Log($"[Respawn] Teleported '{player.name}' to {( _current ? _current.name : "DeathPoint")} at {pos}.");
    }
}
