using Invector;
using UnityEngine;

public class HealthDeathRelay : MonoBehaviour
{
    public vHealthController health;

    void Awake()
    {
        if (!health) health = GetComponent<vHealthController>();
        if (health)
            health.onDead.AddListener(OnDeadForward);
        else
            Debug.LogError("[HealthDeathRelay] vHealthController missing.");
    }

    void OnDeadForward(GameObject go)
    {
        Debug.Log($"[HealthDeathRelay] vHealth.onDead fired for {go.name}");
    }
}

