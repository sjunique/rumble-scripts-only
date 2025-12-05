using UnityEngine;
using System.Collections;

public class WaterFishSpawner : MonoBehaviour
{
    [Tooltip("Drag your fish prefab here")]
    public GameObject fishPrefab;
    [Tooltip("Number of fish to spawn")]
    public int fishCount = 20;

    BoxCollider waterZone;

    void Awake()
    {
        waterZone = GetComponent<BoxCollider>();
        if (!waterZone || !waterZone.isTrigger)
            Debug.LogError("Attach this to your WaterZone collider (trigger)!");
    }

    void Start()
    {
        StartCoroutine(SpawnFish());
    }

    IEnumerator SpawnFish()
    {
        for (int i = 0; i < fishCount; i++)
        {
            Vector3 p = RandomPointInZone();
            Instantiate(fishPrefab, p, Quaternion.Euler(0, Random.Range(0,360), 0));
            yield return null;  // spread over frames
        }
    }

    Vector3 RandomPointInZone()
    {
        var c = waterZone.center + transform.position;
        var s = waterZone.size * 0.5f;
        return new Vector3(
            Random.Range(c.x - s.x, c.x + s.x),
            c.y,                        // fish will swim at water‑plane height; or adjust if they need depth
            Random.Range(c.z - s.z, c.z + s.z)
        );
    }
}
