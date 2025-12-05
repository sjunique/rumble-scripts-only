using UnityEngine;
using System.Collections;
using Invector.vCharacterController;

public class QuestCompletionTeleport : MonoBehaviour
{
    [Header("Which quest completes this jump?")]
    public Quest targetQuest;

 
    [Header("Where to send the player? (choose one)")]
    public string spawnKey;                 // if you use TeleportService + GameLocationManager
    public int spawnPointIndex = -1;        // if you use SpawnPointManager
    public Transform explicitDestination;   // direct Transform fallback

    [Header("Options")]
    public bool stopAutopilotAndAssist = true;
    public bool ensureOnFoot = true;        // exit car if seated
    public float tinyDelay = 0.05f;         // let final UI/FX finish

    bool _done;

    void OnEnable()
    {
        // Fire-and-forget poller (works even if you don't have quest events exposed)
        StartCoroutine(WaitForQuestThenTeleport());
    }

    IEnumerator WaitForQuestThenTeleport()
    {
        if (_done) yield break;

// Wait until QuestManager is ready
    while (QuestManager.Instance == null) yield return null;


        // Wait until quest exists (scene race safety)
        while (targetQuest == null) yield return null;
  // Wait until quest reports completed
    while (!targetQuest.isCompleted) yield return null;
        // Wait until quest is completed
        while (!IsQuestCompleted(targetQuest)) yield return null;

        if (_done) yield break;
        _done = true;

        yield return new WaitForSeconds(tinyDelay);

        // Grab the LIVE player (the cloned instance)
        var link   = PlayerCarLinker.Instance;
        var player = link ? link.player : null;
        if (player == null) { Debug.LogWarning("[QCT] No live player found."); yield break; }

    
        if (ensureOnFoot)
        {
            var enterExit = FindObjectOfType<CarTeleportEnterExit>(true);
            if (enterExit != null)
            {
                // if currently in car, exit back to player mode
                var exit = enterExit.GetType().GetField("inCar",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (exit != null && (bool)exit.GetValue(enterExit) == true)
                {
                    // call ExitCar via coroutine safely if it exists
                    var mi = enterExit.GetType().GetMethod("ExitCar",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null) enterExit.StartCoroutine((IEnumerator)mi.Invoke(enterExit, null));
                }
            }
        }

        // Try teleport using your available systems, in priority order
        if (TryTeleportBySpawnKey(player.transform)) yield break;
        if (TryTeleportBySpawnIndex(player.transform)) yield break;
        if (TryTeleportByTransform(player.transform)) yield break;

        Debug.LogWarning("[QCT] No valid destination found. Provide spawnKey, spawnPointIndex, or explicitDestination.");
    }

    bool IsQuestCompleted(Quest q)
    {
        // Your project already toggles quest.isCompleted — keep it simple
        if (q && q.isCompleted) return true;

        // If you also have a QuestManager API, use it here (optional)
        // var qm = QuestManager.Instance ?? FindObjectOfType<QuestManager>(true);
        // if (qm) return qm.IsCompleted(q);

        return false;
    }

    bool TryTeleportBySpawnKey(Transform player)
    {
        if (string.IsNullOrWhiteSpace(spawnKey)) return false;

        var loc = FindObjectOfType<GameLocationManager>(true);
        if (!loc) return false;

        // Uses the same path your WaterCarSummon uses
        bool ok = TeleportService.TeleportTransformToSpawn(player, spawnKey, loc);
        if (ok)
        {
            PostTeleportCameraReset(player);
            Debug.Log($"[QCT] Teleported player to spawn key '{spawnKey}'.");
        }
        else
        {
            Debug.LogWarning($"[QCT] Teleport to key '{spawnKey}' failed (check GameLocationManager & spawn definitions).");
        }
        return ok;
    }

    bool TryTeleportBySpawnIndex(Transform player)
    {
        if (spawnPointIndex < 0) return false;
        var spm = SpawnPointManager.Instance;
        if (!spm) return false;

        var t = spm.GetSpawnPoint(spawnPointIndex);
        if (!t) { Debug.LogWarning($"[QCT] SpawnPointManager has no index {spawnPointIndex}."); return false; }

       
       
       
       
       
       
       
       
       
        player.SetPositionAndRotation(t.position, t.rotation);
          var upright = player.GetComponent<UprightOnTeleport>();
        if (upright) upright.OnTeleported();
        PostTeleportCameraReset(player);
      
        Debug.Log($"[QCT] Teleported player to SpawnPoint[{spawnPointIndex}] '{t.name}'.");
        return true;
    }

    bool TryTeleportByTransform(Transform player)
    {
        if (!explicitDestination) return false;

        player.SetPositionAndRotation(explicitDestination.position, explicitDestination.rotation);
    var upright = player.GetComponent<UprightOnTeleport>();
        if (upright) upright.OnTeleported();

        PostTeleportCameraReset(player);
        Debug.Log($"[QCT] Teleported player to explicit destination '{explicitDestination.name}'.");
        return true;
    }

    void PostTeleportCameraReset(Transform player)
    {
        // Put the rig back to player mode after any snap
        var rig = FindObjectOfType<CarCameraRig>(true);
        if (rig)
        {
            rig.InitializeForPlayer(player);
            rig.SetMode(CarCameraRig.Mode.Player_Default);
        }
        // optional: zero small velocities if you use Rigidbody
        var rb = player.GetComponent<Rigidbody>();
        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#endif
        }
    }
}
