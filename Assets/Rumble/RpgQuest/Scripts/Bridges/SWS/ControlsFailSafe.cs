// Assets/Rumble/RpgQuest/Bridges/SWS/ControlsFailSafe.cs
using UnityEngine;

public class ControlsFailSafe : MonoBehaviour
{
    [Header("References")]
    public Transform player;                     // drag your player root here

    [Header("Timing")]
    public float reenableDelay = 0.35f;          // seconds input must be off with no AP before restore
    public float poll = 0.15f;

    float timer;

    void Awake()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void OnEnable() => InvokeRepeating(nameof(Tick), poll, poll);
    void OnDisable() => CancelInvoke(nameof(Tick));

    Component FindComp(Transform root, string typeName)
    {
        if (!root) return null;
        var all = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in all)
        {
            var t = c ? c.GetType() : null;
            if (t == null) continue;
            if (t.Name == typeName || t.FullName.EndsWith("." + typeName))
                return c;
        }
        return null;
    }

    bool AnyAutopilotActive()
    {
        // if any SWS driver is alive, we're still in AP
        return FindObjectOfType<SWS.splineMove>() || FindObjectOfType<SWS.navMove>();
    }

    void Tick()
    {
        if (!player) return;

        var input = FindComp(player, "vThirdPersonInput") as Behaviour;
        var ctrl  = FindComp(player, "vThirdPersonController");
        var rb    = player.GetComponent<Rigidbody>();

        bool ap = AnyAutopilotActive();

        // if input is disabled and AP is not running → count and restore
        if (input && !input.enabled && !ap)
        {
            timer += poll;
            if (timer >= reenableDelay)
            {
                // restore input
                input.enabled = true;

                // unlock movement flag if present
                if (ctrl != null)
                {
                    var prop = ctrl.GetType().GetProperty("lockMovement",
                        System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(bool))
                        prop.SetValue(ctrl, false, null);
                }

                // restore physics (turn off kinematic & gravity back on)
                if (rb) { rb.isKinematic = false; rb.useGravity = true; rb.WakeUp(); }

                Debug.Log("[FailSafe] Controls restored after AP/teleport.");
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }
}
