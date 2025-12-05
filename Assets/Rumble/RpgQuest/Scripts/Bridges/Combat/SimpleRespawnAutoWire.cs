using UnityEngine;
using Invector;
using Invector.vCharacterController;



using UnityEngine;
using Invector.vCharacterController;

[DisallowMultipleComponent]
public class SimpleRespawnAutoWire : MonoBehaviour
{
    [Header("Resolved at runtime from clone (optional: set in inspector for testing)")]
    [SerializeField] private SimpleRespawn respawn;
    [SerializeField] private vHealthController health;
    [Tooltip("If true, try to find a SimpleRespawn on this GameObject automatically in Awake.")]
    public bool autoFindRespawn = true;

    // Called in Awake so it works in Play mode quickly.
    void Awake()
    {
        // If inspector assigned it, keep it. Otherwise try to find sensible defaults.
        if (!respawn && autoFindRespawn)
            respawn = GetComponent<SimpleRespawn>();

        // Try to find health on this object or children (includes inactive)
        if (!health)
            health = GetComponentInChildren<vHealthController>(true);

        // If scene respawn exists and we have a health, wire it now (safe no-ops if already wired)
        if (respawn != null && health != null)
        {
            WireEvents(respawn);
        }
    }

    // Public helper called by spawner after clone creation:
    public void AutoWireFromClone(GameObject clone, SimpleRespawn sceneRespawn)
    {
        if (clone == null || sceneRespawn == null) return;

        // prefer health from clone
        health = clone.GetComponentInChildren<vHealthController>(true);
        respawn = sceneRespawn;

        if (health != null && respawn != null)
        {
            WireEvents(respawn);
            Debug.Log("[SimpleRespawnAutoWire] Auto-wired from clone: " + clone.name);
        }
        else
        {
            Debug.LogWarning("[SimpleRespawnAutoWire] AutoWireFromClone could not find required components.");
        }
    }

    public void WireEvents(SimpleRespawn sceneRespawn)
    {
        if (health == null || sceneRespawn == null) return;

        // remove/add to be idempotent
        health.onDead.RemoveListener(sceneRespawn.OnPlayerDead);
        health.onDead.AddListener(sceneRespawn.OnPlayerDead);

        health.onChangeHealth.RemoveListener(OnHealthChanged);
        health.onChangeHealth.AddListener(OnHealthChanged);

        // assign respawn reference
        respawn = sceneRespawn;

        Debug.Log("[SimpleRespawnAutoWire] Wired health events to scene respawn.");
    }

    void OnHealthChanged(float current)
    {
        if (health != null && respawn != null && current <= 0.01f)
            respawn.OnPlayerDead(health.gameObject);
    }
}



/*
[DisallowMultipleComponent]
public class SimpleRespawnAutoWire : MonoBehaviour
{
  [Header("Resolved at runtime from clone (optional: set in inspector for testing)")]
    public SimpleRespawn respawn; // usually the scene respawner instance
    public vHealthController health; // will be set to clone's health

    [Tooltip("If true, auto-wire will attempt to find the SimpleRespawn instance automatically.")]
    public bool autoFindRespawn = true;
    void Awake()
    {
        // DO NOT WIRE EVENTS HERE.
        // Only discover clone health.
        if (!health)
            health = GetComponentInChildren<vHealthController>(true);
    }


    public void WireEvents(SimpleRespawn sceneRespawn)
    {
        if (!health)
        {
            Debug.LogError("[AutoWire] No health component found to wire!");
            return;
        }

        if (!sceneRespawn)
        {
            Debug.LogError("[AutoWire] No SimpleRespawn provided!");
            return;
        }

        respawn = sceneRespawn; // assign final

        // now wire events safely
        health.onDead.RemoveListener(respawn.OnPlayerDead);
        health.onDead.AddListener(respawn.OnPlayerDead);

        health.onChangeHealth.RemoveListener(OnHealthChanged);
        health.onChangeHealth.AddListener(OnHealthChanged);

        Debug.Log("[AutoWire] Events wired: health -> SimpleRespawn.");
    }

    
    
    
    
  /// <summary>
    /// Auto-wire this component to the runtime clone and respawn.
    /// Call this from your spawn code after clone is instantiated.
    /// </summary>
    public void AutoWireFromClone(GameObject cloneRoot, SimpleRespawn knownRespawn = null)
    {
        if (cloneRoot == null)
        {
            Debug.LogError("[SimpleRespawnAutoWire] AutoWireFromClone called with null cloneRoot.");
            return;
        }

        // Resolve respawn reference: prefer provided knownRespawn, else try inspector, else find in scene
        if (knownRespawn != null)
        {
            respawn = knownRespawn;
        }
        else if (respawn == null && autoFindRespawn)
        {
            respawn = FindObjectOfType<SimpleRespawn>();
            if (respawn != null) Debug.Log($"[SimpleRespawnAutoWire] Found scene SimpleRespawn: {respawn.name}");
        }

        // Resolve health on the clone
        var h = cloneRoot.GetComponentInChildren<vHealthController>(true);
        if (h != null)
        {
            health = h;
            Debug.Log($"[SimpleRespawnAutoWire] Resolved health -> {health.name}");
        }
        else
        {
            Debug.LogWarning("[SimpleRespawnAutoWire] Could not resolve vHealthController on clone.");
        }

        // If we have a respawn instance, update its health reference too
        if (respawn != null && health != null)
        {
            respawn.health = health;
            Debug.Log("[SimpleRespawnAutoWire] Pushed clone health into scene respawn.health");
        }

        // If needed, also push thirdPersonController or input (optional)
        var tp = cloneRoot.GetComponentInChildren<vThirdPersonController>(true);
        var inp = cloneRoot.GetComponentInChildren<vThirdPersonInput>(true);
        if (respawn != null)
        {
            if (tp != null) respawn.thirdPersonController = tp;
            if (inp != null) respawn.input = inp;
            Debug.Log("[SimpleRespawnAutoWire] Updated respawn thirdPersonController/input from clone (if found).");
        }
    }








    void Update()                                                           // 3) polling fallback
    {
        if (!health || !respawn) return;
        if (!respawn.enabled) return;

        // if for any reason events didn't fire, catch the dead state
        if (health.isDead)
        {
            Debug.Log("[RespawnWire] Poll detected isDead=true — forcing respawn.");
            respawn.OnPlayerDead(health.gameObject);
        }
    }

    void OnHealthChanged(float current)
    {
        if (!health || !respawn) return;
        if (current <= 0.01f)
        {
            Debug.Log("[RespawnWire] onChangeHealth<=0 — forcing respawn.");
            respawn.OnPlayerDead(health.gameObject);
        }
    }

}
*/