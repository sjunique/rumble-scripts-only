using System.Collections.Generic;
using UnityEngine;

 

public class FishSchoolManager : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject fishPrefab;
    public int fishCount = 20;
    public float spawnRadius = 10f;
    [Range(0.1f, 5f)] public float spawnHeightRange = 2f;

    [Header("Player Interaction")]
    public Transform player;
    public float playerActivationRadius = 25f;
    public float playerFleeRadius = 5f;
    public float fleeIntensity = 2f;

    [Header("School Behavior")]
    public float baseSpeed = 2f;
    public float speedVariation = 0.5f;
    public float rotationSpeed = 5f;
    public float cohesionWeight = 1f;
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;
    public float avoidanceWeight = 2f;
    public float avoidanceDistance = 3f;
    public float patternChangeInterval = 10f;

    [Header("Patterns")]
    public bool usePatterns = true;
    public float patternRadius = 8f;
    public float patternSpeed = 0.5f;

    [Header("Effects")]
    public GameObject schoolVFX;
    public float vfxActivationRadius = 15f;

    public List<FishBoid> fishList = new List<FishBoid>();
    private float nextPatternChangeTime;
    private SchoolPattern currentPattern;
    private Vector3 patternCenter;

    private enum SchoolPattern { Circle, FigureEight, Swirl, Random }

    void Start()
    {
        patternCenter = transform.position;
        nextPatternChangeTime = Time.time + patternChangeInterval;
        currentPattern = SchoolPattern.Circle;
        SpawnFish();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(player.position, transform.position);
        bool shouldActivate = distToPlayer < playerActivationRadius;

        // Manage activation
        if (shouldActivate && fishList.Count == 0)
        {
            SpawnFish();
        }
        SetFishActive(shouldActivate);

        // Manage VFX based on closer distance
        if (schoolVFX) schoolVFX.SetActive(distToPlayer < vfxActivationRadius);

        // Update pattern if needed
        if (usePatterns && Time.time > nextPatternChangeTime)
        {
            currentPattern = (SchoolPattern)Random.Range(0, System.Enum.GetValues(typeof(SchoolPattern)).Length);
            nextPatternChangeTime = Time.time + patternChangeInterval;
            patternCenter = transform.position + Random.insideUnitSphere * spawnRadius;
        }
    }

    public Vector3 GetSchoolPatternTarget(FishBoid fish)
    {
        if (!usePatterns) return transform.position;

        float time = Time.time * patternSpeed;
        Vector3 target = patternCenter;
        float angle = Mathf.Atan2(fish.transform.position.z - patternCenter.z, 
                                fish.transform.position.x - patternCenter.x);

        switch (currentPattern)
        {
            case SchoolPattern.Circle:
                target += new Vector3(
                    Mathf.Cos(time + angle) * patternRadius,
                    0,
                    Mathf.Sin(time + angle) * patternRadius
                );
                break;

            case SchoolPattern.FigureEight:
                target += new Vector3(
                    Mathf.Sin(time * 0.5f) * patternRadius,
                    0,
                    Mathf.Sin(time) * patternRadius * 0.5f
                );
                break;

            case SchoolPattern.Swirl:
                target += new Vector3(
                    Mathf.Cos(time + angle) * patternRadius,
                    Mathf.Sin(time * 2f) * patternRadius * 0.5f,
                    Mathf.Sin(time + angle) * patternRadius
                );
                break;

            case SchoolPattern.Random:
                if (Time.time > nextPatternChangeTime - 1f)
                {
                    target += Random.insideUnitSphere * patternRadius;
                }
                break;
        }

        return target;
    }

    public Vector3 CalculateAvoidance(FishBoid fish)
    {
        Vector3 avoidanceForce = Vector3.zero;
        int avoidanceCount = 0;

        // Obstacle avoidance (simple sphere check)
        Collider[] obstacles = Physics.OverlapSphere(fish.transform.position, avoidanceDistance);
        foreach (var obstacle in obstacles)
        {
            if (obstacle.gameObject != fish.gameObject && !obstacle.CompareTag("Fish"))
            {
                Vector3 avoidDir = fish.transform.position - obstacle.transform.position;
                avoidanceForce += avoidDir.normalized * (avoidanceDistance - avoidDir.magnitude);
                avoidanceCount++;
            }
        }

        // Player avoidance
        if (player != null)
        {
            float playerDist = Vector3.Distance(fish.transform.position, player.position);
            if (playerDist < playerFleeRadius)
            {
                Vector3 fleeDir = fish.transform.position - player.position;
                avoidanceForce += fleeDir.normalized * fleeIntensity * (playerFleeRadius - playerDist);
                avoidanceCount++;
            }
        }

        return avoidanceCount > 0 ? avoidanceForce / avoidanceCount : Vector3.zero;
    }

    void SpawnFish()
    {
        foreach (var fish in fishList)
            if (fish != null) Destroy(fish.gameObject);
        fishList.Clear();

        for (int i = 0; i < fishCount; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            pos.y = Mathf.Clamp(pos.y, transform.position.y - spawnHeightRange, transform.position.y + spawnHeightRange);

            var fishObj = Instantiate(fishPrefab, pos, Quaternion.identity, transform);

            // Vary scale with some larger outliers
            float scale = Random.value < 0.15f ? 
                Random.Range(1.5f, 2.1f) : 
                Random.Range(0.7f, 1.4f);

            fishObj.transform.localScale = Vector3.one * scale;

            var boid = fishObj.GetComponent<FishBoid>();
            if (boid != null)
            {
                boid.manager = this;
                boid.speed = baseSpeed + Random.Range(-speedVariation, speedVariation);
                fishList.Add(boid);
            }
        }
    }

    void SetFishActive(bool active)
    {
        foreach (var fish in fishList)
            if (fish) fish.gameObject.SetActive(active);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerActivationRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerFleeRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patternRadius);
    }
}

/*
public class FishSchoolManager : MonoBehaviour
{
    public GameObject fishPrefab;
    public int fishCount = 20;
    public float spawnRadius = 10f;
    public float playerActivationRadius = 25f;

    public Transform player;
    public List<FishBoid> fishList = new List<FishBoid>();
    //  public GameObject schoolVFX; // Assign your particle prefab in Inspector

    public GameObject schoolVFX; // Assign your particle prefab in Inspector

    void SetFishActive(bool active)
    {
        foreach (var fish in fishList)
            if (fish) fish.gameObject.SetActive(active);
        if (schoolVFX) schoolVFX.SetActive(active); // Toggle effect with school
    }
    void Start()
    {
        // You can leave fish unspawned at start if you want
        SpawnFish();
    }

    void Update()
    {


        if (player == null)
        {
            Debug.LogWarning("FishSchoolManager: Player reference not set!");
            return;
        }
        float dist = Vector3.Distance(player.position, transform.position);
        Debug.Log("Distance to player: " + dist);

        if (dist < playerActivationRadius)
        {
            if (fishList.Count == 0)
                SpawnFish();
            SetFishActive(true);
        }
        else
        {
            SetFishActive(false);
        }





    }


    void SpawnFish()
    {
        // Clean up any previously spawned fish
        foreach (var fish in fishList)
            if (fish != null)
                Destroy(fish.gameObject);
        fishList.Clear();

        for (int i = 0; i < fishCount; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            pos.y = Mathf.Clamp(pos.y, transform.position.y - 2, transform.position.y + 2);

            var fishObj = Instantiate(fishPrefab, pos, Quaternion.identity, transform);

            // Vary scale for natural school (0.7x to 1.4x original)
            // float scale = Random.Range(0.7f, 1.4f);  // Make some fish larger, some smaller
            float scale = Random.value < 0.15f ? Random.Range(1.5f, 2.1f) : Random.Range(0.10f, 1.7f);

            fishObj.transform.localScale = new Vector3(scale, scale, scale);

            var boid = fishObj.GetComponent<FishBoid>();
            if (boid != null)
            {
                boid.manager = this;
                fishList.Add(boid);
            }
        }

    }








    void SetFishActives(bool active)
    {
        foreach (var fish in fishList)
            if (fish) fish.gameObject.SetActive(active);
    }
}
*/
