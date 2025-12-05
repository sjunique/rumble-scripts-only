using UnityEngine;
using System.Collections;
using Invector.vCharacterController;

public static class TeleportService
{
    public static IEnumerator TeleportPlayerWithFX(
        vThirdPersonController player,
        string locationKey,
        GameLocationManager loc,
        ScreenFader fader,
        float lockSeconds = 0.5f,
        GameObject portalVFX = null,
        float vfxLifetime = 1.2f)
    {
        using (var op = Diag.Begin("TP", $"Player→{locationKey}"))
        {
            if (!player || loc == null || string.IsNullOrWhiteSpace(locationKey))
            { Diag.Error("TP", "Missing refs/keys"); yield break; }

            if (portalVFX) Object.Destroy(Object.Instantiate(portalVFX, player.transform.position, Quaternion.identity), vfxLifetime);
            if (fader) yield return fader.FadeOut(0.25f);

            var input = player.GetComponent<vThirdPersonInput>();
            var rb = player.GetComponent<Rigidbody>();
            var col = player.GetComponent<CapsuleCollider>();

            if (input) input.enabled = false;
            if (rb) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            if (col) col.enabled = true;

            bool ok = loc.TeleportPlayer(locationKey, player.transform);
            if (!ok) Diag.Error("TP", $"Teleport failed to '{locationKey}'");

            if (portalVFX) Object.Destroy(Object.Instantiate(portalVFX, player.transform.position, Quaternion.identity), vfxLifetime);

            yield return new WaitForSeconds(lockSeconds);
            if (rb) rb.isKinematic = false;
            if (input) input.enabled = true;

            if (fader) yield return fader.FadeIn(0.25f);
        }
    }

    public static bool TeleportTransformToSpawn(Transform t, string locationKey, GameLocationManager loc)
    {
            if (CarSummonGuard.IsBlocked && t.CompareTag("Vehicle"))
    {
        Diag.Info("LOC", $"Teleport suppressed by CarSummonGuard for <{t.name}>.");
        return false;
    }

        if (!t || loc == null || string.IsNullOrWhiteSpace(locationKey))
        { Diag.Error("TP", "TeleportTransformToSpawn: bad args"); return false; }
   // ⬇️ BLOCK dock teleports right after a NearPlayer summon
    if (CarSummonGuard.IsBlocked && t.CompareTag("Vehicle"))
    {
        Diag.Info("TP", $"Teleport suppressed by CarSummonGuard for '{t.name}'.");
        return false;
    }
        return loc.TeleportTransform(locationKey, t);
    }
}
