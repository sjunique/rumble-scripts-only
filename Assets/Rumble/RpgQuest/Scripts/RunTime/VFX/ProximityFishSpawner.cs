using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FishType
{
    public GameObject prefab;
    public int maxCount = 5;
}

public class ProximityFishSpawner : MonoBehaviour
{
    [Header("Fish Settings")]
    public FishType[] fishTypes;
    public float swimRadius = 8f;
    public float spawnDepth = 1f; // maximum depth for any fish type

    [Header("References")]
    [SerializeField] private BoxCollider waterZoneCollider;

    [Header("Debug")]
    public bool showSpawnGizmos = true;

    private Transform _currentPlayer;
    private float _waterSurfaceY;

    // One list per fish type
    private List<GameObject>[] _spawnedFishGroups;

    void Start()
    {
        if (waterZoneCollider == null)
        {
            Debug.LogError("ProximityFishSpawner: Water Zone collider not assigned!");
            enabled = false;
            return;
        }
        _waterSurfaceY = waterZoneCollider.bounds.max.y;
        Debug.Log($"[FishSpawner] Surface Y = {_waterSurfaceY}");

        // Initialize lists for each fish type
        _spawnedFishGroups = new List<GameObject>[fishTypes.Length];
        for (int i = 0; i < fishTypes.Length; i++)
            _spawnedFishGroups[i] = new List<GameObject>();
    }

    public void SetActivePlayer(Transform player)
    {
        _currentPlayer = player;
        Debug.Log($"[FishSpawner] Active player set to: {player.name}");
    }

    public void SpawnSwimmingFish()
    {
        if (_currentPlayer == null) return;

        for (int i = 0; i < fishTypes.Length; i++)
        {
            var fishType = fishTypes[i];

            // Remove oldest if at cap
            if (_spawnedFishGroups[i].Count >= fishType.maxCount)
            {
                var old = _spawnedFishGroups[i][0];
                if (old != null) Destroy(old);
                _spawnedFishGroups[i].RemoveAt(0);
            }

            // Random point around player, clamped inside waterZoneCollider
            Vector3 center = _currentPlayer.position;
            Vector2 offset2D = Random.insideUnitCircle * swimRadius;
            Vector3 spawnPos = center + new Vector3(offset2D.x, 0f, offset2D.y);

            // Clamp X and Z to collider bounds
            Vector3 boundsMin = waterZoneCollider.bounds.min;
            Vector3 boundsMax = waterZoneCollider.bounds.max;
            spawnPos.x = Mathf.Clamp(spawnPos.x, boundsMin.x, boundsMax.x);
            spawnPos.z = Mathf.Clamp(spawnPos.z, boundsMin.z, boundsMax.z);

            // Random depth (Y)
            float depth = Random.Range(0.5f, spawnDepth);
            spawnPos.y = _waterSurfaceY - depth;
            spawnPos.y = Mathf.Clamp(spawnPos.y, boundsMin.y + 0.1f, _waterSurfaceY - 0.05f);

            if (fishType.prefab != null && waterZoneCollider.bounds.Contains(spawnPos))
            {
                var fish = Instantiate(
                    fishType.prefab,
                    spawnPos,
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    transform
                );
                _spawnedFishGroups[i].Add(fish);

                // Optionally, assign FishBoid manager if present
                var boid = fish.GetComponent<FishBoid>();
                if (boid != null)
                {
                    // Optionally set reference if you have a school manager or want flocking
                    // boid.manager = this; // Only if you add a manager property/logic!
                }
            }
        }
    }

    public void ClearAllFish()
    {
        for (int i = 0; i < _spawnedFishGroups.Length; i++)
        {
            foreach (var fish in _spawnedFishGroups[i])
                if (fish != null) Destroy(fish);
            _spawnedFishGroups[i].Clear();
        }
        Debug.Log("[FishSpawner] Cleared all fish");
    }

    void OnDrawGizmos()
    {
        if (!showSpawnGizmos || _currentPlayer == null || waterZoneCollider == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_currentPlayer.position, swimRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(waterZoneCollider.bounds.center, waterZoneCollider.bounds.size);
    }
}
