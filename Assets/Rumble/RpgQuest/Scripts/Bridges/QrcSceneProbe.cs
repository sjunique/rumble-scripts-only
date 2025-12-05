using UnityEngine;
using System.Text;
using Invector.vCharacterController;

[DefaultExecutionOrder(9999)]
public class QrcSceneProbe : MonoBehaviour
{
    public QuestRouteController qrc;
    public PathFollowerNudge nudge;
    public vThirdPersonController player;

    void Reset()
    {
        qrc    = FindObjectOfType<QuestRouteController>(true);
        nudge  = FindObjectOfType<PathFollowerNudge>(true);
        player = FindObjectOfType<vThirdPersonController>(true);
    }

    void Awake() { if (!qrc) qrc = FindObjectOfType<QuestRouteController>(true); }

    void Update()
    {
        // Press F9 to dump
        if (Input.GetKeyDown(KeyCode.F1)) Dump();
    }

    [ContextMenu("Dump")]
    public void Dump()
    {
        var sb = new StringBuilder();
        sb.AppendLine("==== QRC SCENE PROBE ====");

        // Player
        sb.AppendLine($"Player: {(player ? player.name : "NULL")}");
        if (player)
        {
            var rb = player.GetComponent<Rigidbody>();
            var vInput = player.GetComponent<vThirdPersonInput>();
            sb.AppendLine($"  tag={player.tag}  inputEnabled={(vInput && vInput.enabled)}  rb.kin={(rb && rb.isKinematic)}");
        }

        // Nudge
        sb.AppendLine($"Nudge: {(nudge ? nudge.name : "NULL")}");
        if (nudge)
        {
            var pth = nudge.path;
            sb.AppendLine($"  playerRoot={(nudge.playerRoot ? nudge.playerRoot.name : "NULL")}");
            sb.AppendLine($"  path={(pth ? pth.name : "NULL")}  points={(pth && pth.PathPoints!=null ? pth.PathPoints.Count : 0)}");
            sb.AppendLine($"  autopilot={nudge.autopilot}  nudgeEnabled={nudge.nudgeEnabled}");
        }

        // QRC
        sb.AppendLine($"QRC: {(qrc ? qrc.name : "NULL")}");
        if (qrc)
        {
            sb.AppendLine($"  stages={qrc.stages.Count}");
            for (int i = 0; i < qrc.stages.Count; i++)
            {
                var s = qrc.stages[i];
                var pts = (s.route && s.route.PathPoints!=null) ? s.route.PathPoints.Count : 0;
                var binder = s.entryTrigger ? s.entryTrigger.GetComponent<RouteStageBinder>() : null;

                sb.AppendLine($"  [{i}] '{s.title}'");
                sb.AppendLine($"     route={(s.route? s.route.name : "NULL")}  pts={pts}  routeRoot.active={(s.routeRoot && s.routeRoot.activeInHierarchy)}");
                sb.AppendLine($"     entryTrigger={(s.entryTrigger? s.entryTrigger.name : "NULL")}  binder={(binder? "OK":"MISSING")}");
                sb.AppendLine($"     collectiblesRoot.active={(s.collectiblesRoot && s.collectiblesRoot.gameObject.activeInHierarchy)}  requiredCount={s.requiredCount}");
                sb.AppendLine($"     autoStartAP={s.autoStartAutopilot}  enableNudgeOnStart={s.enableNudgeOnStart}  fromStart={s.fromPathStart}");
            }
        }

        // Obvious red flags
        if (!player) sb.AppendLine("!! No vThirdPersonController found.");
        if (!nudge)  sb.AppendLine("!! No PathFollowerNudge found.");
        if (nudge && (!nudge.playerRoot || !nudge.path))
            sb.AppendLine("!! Nudge missing playerRoot and/or path (AP/nudge will be idle until QRC.SetRoute runs).");

        Debug.Log(sb.ToString());
    }
}
