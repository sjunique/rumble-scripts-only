using UnityEngine;

public class SceneWiringProbe : MonoBehaviour
{
    public QuestRouteController qrc;
    public PathFollowerNudge nudge;
    public RouteStageBinder firstBinder;

    void Awake()
    {
        if (!qrc)   qrc   = FindObjectOfType<QuestRouteController>(true);
        if (!nudge) nudge = FindObjectOfType<PathFollowerNudge>(true);
        if (!firstBinder) firstBinder = FindObjectOfType<RouteStageBinder>(true);

        Debug.Log($"Probe Check [Probe] QRC={(qrc? qrc.name : "null")}  Nudge={(nudge? nudge.name : "null")}  Binder={(firstBinder? firstBinder.name : "null")}");
    }

    void Start()
    {
        if (qrc && qrc.enabled)
            Debug.Log($"[Probe] QRC stages={qrc.stages?.Count ?? 0}");
        if (firstBinder)
            Debug.Log($"binder null [Probe] Binder.controller={(firstBinder.controller? firstBinder.controller.name : "null")} routeRoot={(firstBinder.routeRoot? firstBinder.routeRoot.name:"null")} active={(firstBinder.routeRoot && firstBinder.routeRoot.activeInHierarchy)}");
    }
}
