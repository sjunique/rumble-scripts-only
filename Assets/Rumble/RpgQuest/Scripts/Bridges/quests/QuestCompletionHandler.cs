using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Invector.vCharacterController;
 

public class QuestCompletionHandler : MonoBehaviour
{
    [Header("Target quest & next location")]
    public Quest watchQuest;                  // which quest to react to
    public string nextQuestLocationName;      // spawn key, e.g. "Ocean"

    [Header("Player & FX")]
    public vThirdPersonController player;     // will be auto-found if left empty
    public GameObject portalVFX;
    public ScreenFader fader;
    public float inputLockSecs = 0.5f;
    public float waitBeforeTeleport = 0f;     // optional delay

    bool _teleportQueued;

    // -------- LIFECYCLE --------

    void Awake()
    {
        // Try to bind to the *current* player (spawned clone) ASAP
        TryResolvePlayer();
    }

    void OnEnable()
    {
        if (QuestManager.Instance)
            QuestManager.Instance.QuestCompleted += OnQuestCompleted;

        // static mirror – safe even if Instance is swapped
        QuestManager.QuestCompletedGlobal += OnQuestCompleted;
    }

    void OnDisable()
    {
        if (QuestManager.Instance)
            QuestManager.Instance.QuestCompleted -= OnQuestCompleted;

        QuestManager.QuestCompletedGlobal -= OnQuestCompleted;
    }

    void Start()
    {
        // If we ever want "teleport on load if already complete", we could:
        // if (watchQuest && watchQuest.isCompleted && !_teleportQueued)
        //     StartCoroutine(CompleteQuestSequence());
    }

    // Debug hotkey (optional)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
            StartCoroutine(CompleteQuestSequence());
    }

    // -------- EVENTS --------

    void OnQuestCompleted(Quest q)
    {
        if (_teleportQueued) return;

        if (watchQuest && q != watchQuest)
        {
            Debug.Log($"[QuestDebug] Ignoring completion of '{q?.questName}', watching '{watchQuest.questName}'.");
            return;
        }

        _teleportQueued = true;
        StartCoroutine(CompleteQuestSequence());
    }

    // -------- CORE SEQUENCE --------

    IEnumerator CompleteQuestSequence()
    {
        // 1) Make sure we have the live player
        if (!player)
            TryResolvePlayer();

        if (!player)
        {
            Debug.LogError("[QuestDebug] No player assigned / found for QuestCompletionHandler.");
            yield break;
        }

        // 2) Find GameLocationManager and validate spawn key
        var loc = FindObjectOfType<GameLocationManager>();
        if (!loc)
        {
            Debug.LogError("[QuestDebug] GameLocationManager not found.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(nextQuestLocationName) || !loc.HasSpawn(nextQuestLocationName))
        {
            Debug.LogError($"[QuestDebug] Spawn '{nextQuestLocationName}' not registered. " +
                           $"Keys: {string.Join(", ", loc.GetAllSpawnKeys())}");
            yield break;
        }

        // 3) FX before teleport
        if (portalVFX)
            Destroy(Instantiate(portalVFX, player.transform.position, Quaternion.identity), 1.2f);

        if (waitBeforeTeleport > 0f)
            yield return new WaitForSeconds(waitBeforeTeleport);

        // 4) Teleport using your shared TeleportService
        yield return TeleportService.TeleportPlayerWithFX(
            player,
            nextQuestLocationName,
            loc,
            fader,
            inputLockSecs,
            portalVFX,
            1.2f
        );

        // 5) AFTER teleport, overwrite the scene checkpoint so SceneSpawnRouter uses it on restart
        var sceneName = SceneManager.GetActiveScene().name;
        SaveService.SaveSceneCheckpoint(sceneName, player.transform.position, player.transform.rotation);
        Debug.Log($"[QuestDebug] Saved checkpoint after quest complete at {player.transform.position} for scene '{sceneName}'.");
    }

    // -------- HELPERS --------

    void TryResolvePlayer()
    {
        if (player) return;

        // If you’re using PlayerSpawnBroadcaster, prefer that
        if (PlayerSpawnBroadcaster.Last != null)
        {
            var tpc = PlayerSpawnBroadcaster.Last.GetComponent<vThirdPersonController>();
            if (tpc)
            {
                player = tpc;
                Debug.Log("[QuestDebug] Bound player from PlayerSpawnBroadcaster.");
                return;
            }
        }

        // Fallback – first vThirdPersonController in scene
        var found = FindObjectOfType<vThirdPersonController>();
        if (found)
        {
            player = found;
            Debug.Log("[QuestDebug] Bound player via FindObjectOfType<vThirdPersonController>.");
        }
    }
}
