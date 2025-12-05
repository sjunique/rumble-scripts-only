// WaterZone.cs
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class WaterZone : MonoBehaviour
{
    [Header("Settings")]
    public float swimFishInterval = 3f;
    public bool debugLogs = true;

    [Header("References")]
    [SerializeField] private ProximityFishSpawner spawner;

    private bool _playerInside;

    void Reset()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        var rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Awake()
    {
        if (spawner == null)
            Debug.LogError("WaterZone: ProximityFishSpawner not assigned!", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
        if (debugLogs) Debug.Log($"[WaterZone] Player entered: {other.name}");
        spawner.SetActivePlayer(other.transform);

        // spawn one immediately
        spawner.SpawnSwimmingFish();
        // then repeat at set interval
        InvokeRepeating(nameof(TickSpawn), swimFishInterval, swimFishInterval);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
        if (debugLogs) Debug.Log($"[WaterZone] Player exited");
        CancelInvoke(nameof(TickSpawn));
        spawner.ClearAllFish();
    }

    private void TickSpawn()
    {
        if (!_playerInside) return;
        if (debugLogs) Debug.Log("[WaterZone] TickSpawn");
        spawner.SpawnSwimmingFish();
    }
}
