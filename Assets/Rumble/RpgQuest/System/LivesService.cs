// Assets/Rumble/System/LivesService.cs
using UnityEngine;
using System;



using System;
using UnityEngine;

public class LivesService : MonoBehaviour
{
    [Header("Lives Settings")]
    [Tooltip("Starting lives per run")]
    public int MaxLives = 3;

    public int CurrentLives { get; private set; }

    public event Action OnOutOfLives;
public static LivesService Instance { get; private set; }
public event Action<int> OnLivesChanged; // new for HUD


    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        ResetLives();
        NotifyChange();
    }

void NotifyChange()
{
    OnLivesChanged?.Invoke(CurrentLives);
}



public void LoseLife()
{
    if (CurrentLives <= 0)
    {
        Debug.LogWarning("[LivesService] LoseLife() called with 0 lives.");
        return;
    }

    CurrentLives--;
    Debug.Log($"[LivesService] Life lost → {CurrentLives} remaining.");
    NotifyChange();

    if (CurrentLives <= 0)
    {
        Debug.LogWarning("[LivesService] ⚠ Out of lives!");
        OnOutOfLives?.Invoke();
    }
}


    public void ResetLives()
    {
        CurrentLives = MaxLives;
//        Debug.Log($"[LivesService] Reset to {CurrentLives} lives.");
    }
public void AddLife(int amount = 1)
{
    CurrentLives = Mathf.Min(CurrentLives + amount, MaxLives);
    Debug.Log($"[LivesService] Added {amount} life → now {CurrentLives}.");
    NotifyChange();
}

}


// [DefaultExecutionOrder(-1000)]
// public class LivesService : MonoBehaviour
// {
//     public static LivesService Instance { get; private set; }

//     [Header("Config")]
//     [Min(1)] public int startingLives = 3;
//     public bool carryAcrossLevels = true; // if false, reset on each level load

//     [Header("Runtime (read-only)")]
//     [SerializeField] private int currentLives;

//     public int CurrentLives => currentLives;
//     public event Action<int> OnLivesChanged;    // new value
//     public event Action OnOutOfLives;           // fired when hits zero

//     void Awake()
//     {
//         if (Instance && Instance != this) { Destroy(gameObject); return; }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);

//         ResetLivesIfNeeded();
//     }

//     public void ResetLivesIfNeeded()
//     {
//         if (currentLives <= 0 || !carryAcrossLevels)
//             SetLives(startingLives);
//     }

//     public void SetLives(int value)
//     {
//         currentLives = Mathf.Max(0, value);
//         OnLivesChanged?.Invoke(currentLives);
//         if (currentLives == 0) OnOutOfLives?.Invoke();
//     }

//     public void AddLife(int amount = 1) => SetLives(currentLives + Mathf.Max(0, amount));
//     public void LoseLife(int amount = 1)
//     {
//         if (amount <= 0) return;
//         SetLives(currentLives - amount);
//     }

//     /// Call this when the player dies.
//     public void HandlePlayerDeath()
//     {
//         LoseLife(1);
//         if (currentLives > 0)
//         {
//             // Try checkpoint first; if none, reset to spawn
//             var svc = FindObjectOfType<PauseOverlayController>(true); // just to reuse the same helpers, or:
//             TryRespawn();
//         }
//         // if zero, OnOutOfLives has fired; GameFlow should handle Game Over
//     }

//     void TryRespawn()
//     {
//         // Use your SaveService & GameLocationManager directly:
//         var playerGO = GameObject.FindGameObjectWithTag("Player");
//         if (!playerGO) { Debug.LogWarning("[Lives] No Player to respawn."); return; }

//         // Prefer checkpoint if exists, else spawn
//         if (SaveService.TryLoadSceneCheckpoint(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, out var cp) && cp != null && cp.has)
//         {
//             playerGO.transform.SetPositionAndRotation(cp.pos, cp.rot);
//             Debug.Log("[Lives] Respawned at checkpoint.");
//         }
//         else
//         {
//             var loc = FindObjectOfType<GameLocationManager>(true);
//             if (loc) loc.TeleportPlayer("PlayerSpawnPoint", playerGO.transform);
//             Debug.Log("[Lives] Respawned at spawn.");
//         }
//     }
// }
