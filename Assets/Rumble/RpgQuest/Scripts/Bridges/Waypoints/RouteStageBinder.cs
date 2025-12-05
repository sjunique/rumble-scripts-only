using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RouteStageBinder : MonoBehaviour
{
    public QuestRouteController controller;
    public int stageIndex = 0;

    [Header("Reveal on bind")]
    public GameObject routeRoot;
    public GameObject collectiblesRoot;
    public CollectiblePathPopulator populator;

    [Header("Behaviour")]
    public string playerTag = "Player";
    public bool oneShot = true;
    public float cooldown = 0.75f;

    bool _busy, _used;
    float _busyUntil = 0f;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void Update()
    {
        if (_busy && Time.time >= _busyUntil)
            _busy = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_busy || _used) return;
        if (!other.transform.root.CompareTag(playerTag)) return;

        // Runtime trigger crossing → GO is active, so a coroutine is fine here.
        StartCoroutine(BindRoutine());
    }

    System.Collections.IEnumerator BindRoutine()
    {
        _busy = true;

        RevealNow();
        BeginStageNow();

        if (oneShot)
        {
            _used = true;
            gameObject.SetActive(false);
        }
        else
        {
            _busyUntil = Time.time + cooldown;
            yield return null; // keep it simple
        }
    }

    /// <summary>
    /// Safe to call from UI even if this GO is (or will be) deactivated this frame.
    /// No coroutines here.
    /// </summary>
    public void TriggerNow(bool reveal = true)
    {
        if (_used) return;

        if (reveal) RevealNow();
        BeginStageNow();

        if (oneShot)
        {
            _used = true;
            gameObject.SetActive(false);
        }
        else
        {
            _busy = true;
            _busyUntil = Time.time + cooldown;  // time-based cooldown, no StartCoroutine
        }
    }

 public void RevealNow()
{
    if (routeRoot)
    {
        routeRoot.SetActive(true);
        Debug.Log($"[Binder] Enabling routeRoot={routeRoot.name} activeNow={routeRoot.activeSelf}", this);
    }
    if (collectiblesRoot)
    {
        collectiblesRoot.SetActive(true);
        Debug.Log($"[Binder] Enabling collectiblesRoot={collectiblesRoot.name} activeNow={collectiblesRoot.activeSelf}", this);
    }
}


    void BeginStageNow()
    {
        if (controller) controller.BeginStage(stageIndex);
        else Debug.LogWarning($"[Binder] No QuestRouteController on {name}", this);
    }
}
