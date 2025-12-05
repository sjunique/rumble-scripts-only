using UnityEngine;
using System.Collections;

public class MountTeleporter : MonoBehaviour
{
    [Header("FX (optional)")]
    [SerializeField] private GameObject portalFX;
    [SerializeField] private float fxBeat = 0.3f;   // small delay between source/destination FX

    public IEnumerator MoveToMount(Transform who, Transform mount, bool parentToMount, float upOffset)
    {
        if (!who || !mount) yield break;

        SpawnFX(who.position);
        yield return new WaitForSeconds(fxBeat);

        // snap & (optionally) parent
        var dst = mount.position + Vector3.up * upOffset;
        who.SetPositionAndRotation(dst, mount.rotation);
        if (parentToMount) who.SetParent(mount, true);

        SpawnFX(dst);
    }

    public IEnumerator MoveOffMount(Transform who, Transform exitPoint, float upOffset)
    {
        if (!who) yield break;

        SpawnFX(who.position);

        // Always unparent first
        who.SetParent(null, true);

        yield return new WaitForSeconds(fxBeat);

        // Safe fallback if no exit transform assigned
        Vector3 dst = exitPoint ? exitPoint.position : who.position + who.right * 1.5f;
        dst += Vector3.up * upOffset;

        who.SetPositionAndRotation(dst, exitPoint ? exitPoint.rotation : who.rotation);
        SpawnFX(dst);
    }

    void SpawnFX(Vector3 pos)
    {
        if (!portalFX) return;
        var o = Instantiate(portalFX, pos, Quaternion.identity);
        Destroy(o, fxBeat * 2f);
    }
}
