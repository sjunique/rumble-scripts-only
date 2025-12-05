 using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_VISUAL_EFFECT_GRAPH
using UnityEngine.VFX;
#endif

/// <summary>
/// Turns on/off a VFX when the PLAYER comes close to the collectible.
/// Works with spawned players by searching by tag in a loop.
/// </summary>
public class CollectibleVFXActivator : MonoBehaviour
{
    // ---------------- Events ----------------
    [Header("Events")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    // ---------------- VFX ----------------
    [Header("VFX")]
    [Tooltip("Prefab (asset) or scene instance. If prefab, it will be instantiated.")]
    public GameObject vfxPrefabOrInstance;
    public Transform vfxParent;

    // ---------------- Player resolve ----------------
    [Header("Player Resolve")]
    public Transform player;
    public bool searchForPlayerIfNull = true;
    public string playerTag = "Player";
    [Range(0.1f, 5f)] public float playerRetryInterval = 0.5f;

    // ---------------- Proximity trigger ----------------
    [Header("Player Proximity Trigger")]
    public bool enablePlayerRadius = true;
    [Tooltip("Distance at which VFX turns ON.")]
    public float activationRadius = 10f;
    [Tooltip("How much further away the player must move before VFX turns OFF.")]
    [Range(0f, 5f)] public float exitBuffer = 1.0f;
    public bool deactivateWhenFar = true;

    // ---------------- Playback prefs ----------------
    [Header("Playback")]
    [Tooltip("If true, keep GO active and control ParticleSystems/VFX via Play/Stop.")]
    public bool playStopOnly = true;
    [Tooltip("On activate, rewind all systems so bursts refire.")]
    public bool forceRewindOnActivate = true;

    [Header("Debug")]
    public bool verbose = false;

    // internals
    GameObject _vfxInstance;
    ParticleSystem[] _particles;
#if UNITY_VISUAL_EFFECT_GRAPH
    VisualEffect[] _visualEffects;
#endif
    bool _isActive;
    float _lastDist;
    Coroutine _playerFinderCo;

    void Start()
    {
        if (!vfxParent) vfxParent = transform;

        InitPlayer();
        InitVFX();
    }

    void OnEnable()
    {
        if (_playerFinderCo == null && searchForPlayerIfNull && player == null)
            _playerFinderCo = StartCoroutine(Co_FindPlayerLoop());
    }

    void OnDisable()
    {
        if (_playerFinderCo != null) StopCoroutine(_playerFinderCo);
        _playerFinderCo = null;
    }

    // ---------------- Init ----------------
    void InitPlayer()
    {
        if (player == null) FindPlayerByTag();

        if (player == null && searchForPlayerIfNull && _playerFinderCo == null)
            _playerFinderCo = StartCoroutine(Co_FindPlayerLoop());
    }

    void InitVFX()
    {
        if (vfxPrefabOrInstance != null && vfxPrefabOrInstance.scene.IsValid())
            _vfxInstance = vfxPrefabOrInstance;                           // scene instance
        else if (vfxPrefabOrInstance != null)
            _vfxInstance = Instantiate(vfxPrefabOrInstance, vfxParent);   // prefab asset
        else if (transform.childCount > 0)
            _vfxInstance = transform.GetChild(0).gameObject;              // first child

        if (_vfxInstance == null)
        {
            Debug.LogWarning("CollectibleVFXActivator: No VFX found.", this);
            return;
        }

        _particles = _vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
#if UNITY_VISUAL_EFFECT_GRAPH
        _visualEffects = _vfxInstance.GetComponentsInChildren<VisualEffect>(true);
#endif

        if (!playStopOnly)
            _vfxInstance.SetActive(false);

        SetVFXActive(false, immediate: true);
    }

    // ---------------- Update ----------------
    void Update()
    {
        if (_vfxInstance == null) return;

        // keep trying to bind to a spawned player
        if (player == null && searchForPlayerIfNull && _playerFinderCo == null)
            _playerFinderCo = StartCoroutine(Co_FindPlayerLoop());

        bool playerOK = false;

        if (enablePlayerRadius && player != null)
        {
            _lastDist = Vector3.Distance(player.position, transform.position);
            playerOK = _lastDist <= activationRadius;
        }

        bool shouldActivate = ComputeShouldActivate(playerOK);

        if (!_isActive && shouldActivate)
            SetVFXActive(true);
        else if (_isActive && deactivateWhenFar && !shouldActivate)
            SetVFXActive(false);

        if (verbose)
        {
            int alive = 0;
            if (_particles != null)
                foreach (var ps in _particles)
                    if (ps) alive += ps.particleCount;

            Debug.Log($"[CollectibleVFX] Active={_isActive} Dist={_lastDist:F2} Alive={alive}", this);
        }
    }

    bool ComputeShouldActivate(bool playerOKEnter)
    {
        if (!enablePlayerRadius) return false;

        if (!_isActive)
        {
            // currently OFF → require enter radius
            return playerOKEnter;
        }
        else
        {
            // currently ON → stay on until player is beyond exit radius
            if (!deactivateWhenFar) return true;

            float exitRadius = activationRadius + exitBuffer;
            bool playerStillClose = (player != null &&
                                     Vector3.Distance(player.position, transform.position) <= exitRadius);
            return playerStillClose;
        }
    }

    // ---------------- Play/Stop ----------------
    void SetVFXActive(bool active, bool immediate = false)
    {
        if (_isActive == active && !immediate) return;

        _isActive = active;

        if (!playStopOnly && _vfxInstance != null)
            _vfxInstance.SetActive(active);

        if (active)
        {
            onActivated?.Invoke();
        }
        else
        {
            onDeactivated?.Invoke();
        }

        if (_particles != null && _particles.Length > 0)
        {
            foreach (var ps in _particles)
            {
                if (!ps) continue;
                if (active)
                {
                    if (forceRewindOnActivate)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Simulate(0f, true, true, true);
                    }
                    ps.Play(true);
                }
                else
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

#if UNITY_VISUAL_EFFECT_GRAPH
        if (_visualEffects != null && _visualEffects.Length > 0)
        {
            foreach (var vfx in _visualEffects)
            {
                if (!vfx) continue;
                if (active)
                {
                    if (forceRewindOnActivate) vfx.Reinit();
                    vfx.Play();
                }
                else
                {
                    vfx.Stop();
                }
            }
        }
#endif
    }

    // ---------------- Player finders ----------------
    IEnumerator Co_FindPlayerLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.1f, playerRetryInterval));
        while (player == null)
        {
            FindPlayerByTag();
            yield return wait;
        }
        _playerFinderCo = null;
    }

    void FindPlayerByTag()
    {
        if (string.IsNullOrEmpty(playerTag)) return;

        var obj = GameObject.FindGameObjectWithTag(playerTag);
        if (obj != null && obj.activeInHierarchy)
            player = obj.transform;
    }

    /// <summary>
    /// For spawn systems: call this after instantiating the player.
    /// </summary>
    public void SetPlayer(Transform t)
    {
        player = t;
    }

    void OnDrawGizmosSelected()
    {
        if (enablePlayerRadius)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, activationRadius);
            if (exitBuffer > 0f)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
                Gizmos.DrawWireSphere(transform.position, activationRadius + exitBuffer);
            }
        }
    }
}
