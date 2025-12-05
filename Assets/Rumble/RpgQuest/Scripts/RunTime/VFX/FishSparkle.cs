using UnityEngine;

public class FishSparkle : MonoBehaviour
{
    public ParticleSystem sparklePrefab;
    ParticleSystem sparkleInstance;

    void Start()
    {
        if (sparklePrefab != null)
            sparkleInstance = Instantiate(sparklePrefab, transform);
    }
}
