using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestRouteController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Data
    // ─────────────────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class Stage
    {
        [Header("On Complete → Transition")]
        [Tooltip("If true, after this stage completes (and optional teleport finishes), start the next stage.")]
        public bool beginNextAfterTeleport = true;

        [Tooltip("-1 = use currentIndex + 1. Otherwise explicitly start this stage index.")]
        public int nextStageIndex = -1;

        [Header("Stage")]
        public string title;
        public WaypointPathVisualizer route;   // optional (we’ll also search under routeRoot)
        public GameObject routeRoot;           // visual wrapper you toggle on/off

        [Header("Teleport on Complete (optional)")]
        public bool teleportOnComplete = false;
        public string teleportLocationKey = "Ocean";

        [Header("Entry")]
        public GameObject entryTrigger;        // add RouteStageBinder here
        public bool autoStartAutopilot = true;
        public bool enableNudgeOnStart = true;
        public bool fromPathStart = true;

        [Header("Collectibles (completion)")]
        public Transform collectiblesRoot;
        public bool completeOnRouteEnd = false;
        public string collectibleTag = "Collectible";
        public string nameContains = "collectible";
        public int requiredCount = -1;         // -1 => auto count on begin

        [Header("On Complete")]
        public bool deactivateRouteOnComplete = true;
        public GameObject[] enableOnComplete;
        public GameObject[] disableOnComplete;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Refs / State
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Refs")]
    public PathFollowerNudge nudge;

    [Header("Teleport Refs (optional)")]
    public Invector.vCharacterController.vThirdPersonController player;
    public GameLocationManager loc;
    public ScreenFader fader;
    public GameObject portalVFX;
    public float teleportLockSeconds = 0.5f;

    [Header("Stages (in order)")]
    public List<Stage> stages = new();

    [Header("Behaviour")]
    public bool autoStartFirstOnPlay = false;
    public float pollInterval = 0.3f;

    public static QuestRouteController Instance { get; private set; }

    int current = -1;
    Coroutine pollCo;

    // ─────────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // ✅ proper singleton init
        Instance = this;
var nudge = FindObjectOfType<PathFollowerNudge>(true);
if (!nudge)
{
    Debug.Log("[QRC] No PathFollowerNudge found (OK: using SWS).");
    // do NOT return; QRC can still handle stages/collectibles
}
        if (nudge != null)
            nudge.OnAutopilotFinished += HandleAutopilotFinished;

        // Ensure triggers are wired
        for (int i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (!s.entryTrigger) continue;

            var binder = s.entryTrigger.GetComponent<RouteStageBinder>();
            if (!binder) binder = s.entryTrigger.AddComponent<RouteStageBinder>();
            binder.controller = this;
            binder.stageIndex = i;
            binder.playerTag = "Player";
            binder.oneShot = false; // make true if you want one-time bind
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (nudge != null) nudge.OnAutopilotFinished -= HandleAutopilotFinished;
    }

    void Start()
    {
        if (autoStartFirstOnPlay && stages.Count > 0)
            BeginStage(0);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────────
    public void BeginStage(int index)
    {
        if (index < 0 || index >= stages.Count)
        {
            Debug.LogWarning($"[QRC] BeginStage invalid index {index}");
            return;
        }

        // clear previous
        if (nudge) nudge.ClearRoute(true);
        if (pollCo != null) { StopCoroutine(pollCo); pollCo = null; }

        current = index;
        var s = stages[current];

        if (s.routeRoot) s.routeRoot.SetActive(true);
        if (s.route) s.route.gameObject.SetActive(true);

        // find a path even if Stage.route is null (look under routeRoot)
        var route = ResolveRoute(s);
        if (!route)
        {
            Debug.LogError($"[QRC] BeginStage {index}: No WaypointPathVisualizer found. " +
                           $"Assign Stage.route or place one under routeRoot.", this);
            return;
        }

        // set required collectibles (once at stage begin)
        if (s.requiredCount < 0)
            s.requiredCount = CountActiveCollectibles(s);

        // bind to nudge
        if (nudge)
        {
            nudge.autoDisableOnFinish = false; // we’ll explicitly complete
            nudge.SetRoute(route,
                           enableNudge: s.enableNudgeOnStart,
                           startAutopilot: s.autoStartAutopilot,
                           fromStart: s.fromPathStart);

            // optional kickstarter if you added it to the player
            var kicker = player ? player.GetComponent<AutopilotKickStarter>() : null;
            if (kicker) kicker.Kick(0.25f);

            Debug.Log($"[QRC] BeginStage {index}: route={route.name} nudge={s.enableNudgeOnStart} AP={s.autoStartAutopilot} fromStart={s.fromPathStart}", this);
        }

        // start polling collectibles
        pollCo = StartCoroutine(PollStageCompletion());

        Debug.Log($"[QRC] >>> Stage {current} '{s.title}' begun. Need {s.requiredCount} collectibles.");
    }

    public void CompleteCurrentStage()
    {
        if (current < 0 || current >= stages.Count) return;
        StartCoroutine(CompleteStageRoutine(current));
    }

    public void NotifyCollectibleCollected()
    {
        if (current < 0) return;
        var s = stages[current];
        s.requiredCount = Mathf.Max(0, s.requiredCount - 1);
        if (s.requiredCount == 0)
            CompleteCurrentStage();
    }

    public bool TryCompleteIfNoCollectiblesLeft()
    {
        if (current < 0) return false;
        var s = stages[current];
        int remaining = CountActiveCollectibles(s);
        if (remaining <= 0)
        {
            CompleteCurrentStage();
            return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────────────────────────
    WaypointPathVisualizer ResolveRoute(Stage s)
    {
        if (s.route) return s.route;
        if (s.routeRoot) return s.routeRoot.GetComponentInChildren<WaypointPathVisualizer>(true);
        return null;
    }

    int ResolveNextStageIndex(int finishedIndex, Stage s)
    {
        if (s == null) return -1;
        if (s.nextStageIndex >= 0 && s.nextStageIndex < stages.Count)
            return s.nextStageIndex;

        int idx = finishedIndex + 1;
        return (idx < stages.Count) ? idx : -1;
    }

    IEnumerator CompleteStageRoutine(int finishedIndex)
    {
        if (finishedIndex < 0 || finishedIndex >= stages.Count) yield break;

        var s = stages[finishedIndex];

        // stop route influence
        if (nudge) nudge.ClearRoute(true);

        // visuals
        if (s.deactivateRouteOnComplete)
        {
            if (s.route) s.route.gameObject.SetActive(false);
            if (s.routeRoot) s.routeRoot.SetActive(false);
        }

        // stop polling
        if (pollCo != null) { StopCoroutine(pollCo); pollCo = null; }

        Debug.Log($"[QRC] <<< Stage {finishedIndex} '{s.title}' complete.");

        // compute next before we change state
        int nextIndex = ResolveNextStageIndex(finishedIndex, s);

        // optional teleport for THIS stage
        bool didTeleport = false;
        if (s.teleportOnComplete && player)
        {
            var mgr = loc ? loc : FindObjectOfType<GameLocationManager>();
            if (!mgr)
            {
                Debug.LogWarning("[QRC] No GameLocationManager in scene; skipping teleport.");
            }
            else if (string.IsNullOrWhiteSpace(s.teleportLocationKey))
            {
                Debug.LogWarning("[QRC] teleportOnComplete is true but teleportLocationKey is empty.");
            }
            else
            {
                Debug.Log($"[QRC] Teleporting player → '{s.teleportLocationKey}'");
                yield return TeleportService.TeleportPlayerWithFX(
                    player, s.teleportLocationKey, mgr, fader,
                    teleportLockSeconds, portalVFX, 1.2f);
                didTeleport = true;
            }
        }

        // go idle by default
        current = -1;

        // start next if requested AND valid AND not self-looping
        if (s.beginNextAfterTeleport)
        {
            if (nextIndex < 0 || nextIndex >= stages.Count || nextIndex == finishedIndex)
            {
                Debug.LogWarning("[QRC] Not starting next stage: invalid index or self-loop prevented.");
                yield break;
            }

            if (didTeleport) yield return null; // let transforms settle
            BeginStage(nextIndex);
        }
    }

    IEnumerator PollStageCompletion()
    {
        while (current >= 0)
        {
            var s = stages[current];

            // If configured to complete on route end, HandleAutopilotFinished will call Complete.
            // Here we watch the collectible requirement.
            int remaining = Mathf.Max(0, CountActiveCollectibles(s));
            if (s.requiredCount >= 0 && remaining <= 0)
            {
                CompleteCurrentStage();
                yield break;
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    void HandleAutopilotFinished()
    {
        if (current < 0) return;
        var idx = current;
        var s = stages[idx];
        if (s.completeOnRouteEnd)
        {
            // Call the routine with a stable index (don't use `current` after this)
            StartCoroutine(CompleteStageRoutine(idx));
        }
        else
        {
            if (nudge) nudge.autopilot = false; // stop AP, keep gentle nudge
        }
    }

    int CountActiveCollectibles(Stage s)
    {
        if (!s.collectiblesRoot) return 0;

        int count = 0;
        string tag = s.collectibleTag ?? "";
        string needle = (s.nameContains ?? "").ToLowerInvariant();

        var tfs = s.collectiblesRoot.GetComponentsInChildren<Transform>(true);
        foreach (var tf in tfs)
        {
            if (tf == s.collectiblesRoot) continue;
            var go = tf.gameObject;
            if (!go.activeInHierarchy) continue;

            bool tagHit = !string.IsNullOrEmpty(tag) && go.CompareTag(tag);
            bool nameHit = string.IsNullOrEmpty(needle) || go.name.ToLowerInvariant().Contains(needle);

            if (tagHit || nameHit)
            {
                var r = go.GetComponent<Renderer>();
                if (r && r.enabled) count++;
            }
        }
        return count;
    }
}
