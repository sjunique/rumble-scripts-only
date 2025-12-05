using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class TriggerKickOnEnable : MonoBehaviour
{
    [Header("Who to call when already inside")]
    [Tooltip("Optional: drag the script that normally handles this trigger (e.g., your RouteTrigger component).")]
    public MonoBehaviour triggerHandler;

    [Tooltip("Public method name to call on the handler (default: Activate).")]
    public string handlerMethodName = "Activate";

    [Header("Player settings")]
    public string playerTag = "Player";
    public bool nudgePlayerIfInside = true;

    Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;

        // Diags
        var parentActive = transform.parent == null || transform.parent.gameObject.activeInHierarchy;
        Debug.Log($"[TKOE] {name} Awake. isTrigger={_col.isTrigger}, layer={gameObject.layer}, parentActive={parentActive}, activeSelf={gameObject.activeSelf}");
    }

    void OnEnable()
    {
        Debug.Log($"[TKOE] {name} OnEnable. activeInHierarchy={gameObject.activeInHierarchy}");
        StartCoroutine(CheckAndKickNextFrame());
    }

    IEnumerator CheckAndKickNextFrame()
    {
        // let transforms/colliders settle
        yield return null;
        Physics.SyncTransforms();

        if (!_col)
        {
            Debug.LogWarning($"[TKOE] {name} has no Collider?!");
            yield break;
        }

        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (!player)
        {
            Debug.LogWarning("[TKOE] No object with tag 'Player' found.");
            yield break;
        }

        var pCol = player.GetComponent<Collider>();
        if (!pCol)
        {
            Debug.LogWarning("[TKOE] Player has no Collider.");
            yield break;
        }

        bool overlapped = Physics.ComputePenetration(
            _col, transform.position, transform.rotation,
            pCol, pCol.transform.position, pCol.transform.rotation,
            out Vector3 dir, out float dist
        );

        Debug.Log($"[TKOE] Overlap check → {overlapped} (dist={dist:0.000})");

        if (!overlapped) yield break;

        // Prefer direct handler call
        if (triggerHandler && !string.IsNullOrEmpty(handlerMethodName))
        {
            var mi = triggerHandler.GetType().GetMethod(handlerMethodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (mi != null)
            {
                Debug.Log($"[TKOE] Calling {triggerHandler.GetType().Name}.{handlerMethodName}()");
                mi.Invoke(triggerHandler, null);
                yield break;
            }
        }

        // Fallback: nudge player slightly to force an enter event
        if (nudgePlayerIfInside)
        {
            var rb = player.GetComponent<Rigidbody>();
            var p  = player.transform.position;
            Vector3 epsilon = dir.normalized * (dist + 0.02f);
            if (rb) rb.position = p + epsilon;
            else    player.transform.position = p + epsilon;
            Debug.Log("[TKOE] Nudged player to force OnTriggerEnter.");
        }
    }
}
