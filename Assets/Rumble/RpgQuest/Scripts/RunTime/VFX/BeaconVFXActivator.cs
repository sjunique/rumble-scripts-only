using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_VISUAL_EFFECT_GRAPH
using UnityEngine.VFX;
#endif
using System.Collections;
using UnityEngine;
#if UNITY_VISUAL_EFFECT_GRAPH
using UnityEngine.VFX;
#endif

public class BeaconVFXActivator : MonoBehaviour
{
    [Header("Hovercar Proximity (car must also be near the beacon)")]
    public bool useHoverHorizontalRadius = true;
    public float hoverEnterRadius = 12f;
    public float hoverExitRadius = 14f;   // slightly larger to avoid flicker



    // inside your BeaconVFXActivator class
    [Header("Events")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;


    // ---------------- References ----------------
    [Header("VFX")]
    [Tooltip("Prefab (asset) or scene instance. If prefab, it will be instantiated.")]
    public GameObject vfxPrefabOrInstance;
    public Transform vfxParent;

    [Header("Player Resolve")]
    public Transform player;
    public bool searchForPlayerIfNull = true;
    public string playerTag = "Player";
    [Range(0.1f, 5f)] public float playerRetryInterval = 0.5f;

    [Header("Hovercar Resolve")]
    public Transform hovercar;
    public bool autoFindHovercar = true;
    public string hovercarTag = "Vehicle"; // or "Car" if you prefer
    [Range(0.1f, 5f)] public float hoverRetryInterval = 0.5f;

    // ---------------- Triggers ----------------
    public enum TriggerMode { Any, All }         // Any = player OR hovercar; All = both
    [Header("Trigger Logic")]
    public TriggerMode triggerMode = TriggerMode.Any;

    [Header("Player Proximity Trigger")]
    public bool enablePlayerRadius = true;
    public float activationRadius = 10f;
    [Range(0f, 5f)] public float exitBuffer = 1.0f;
    public bool deactivateWhenFar = true;

    public enum AltitudeMode { WorldY, AboveBeaconY, AboveTerrain, AboveSurfaceRaycast }

    [Header("Hovercar Altitude Trigger")]
    public bool enableHoverAltitude = true;
    public AltitudeMode altitudeMode = AltitudeMode.AboveBeaconY;
    [Tooltip("Enter when altitude >= enterHeight; exit when altitude <= exitHeight (hysteresis).")]
    public float enterHeight = 8f;
    public float exitHeight = 6f; // should be lower than enterHeight to prevent flicker

    [Tooltip("Layer mask for AboveSurfaceRaycast mode (e.g., Ground/Water).")]
    public LayerMask surfaceMask = ~0;
    [Tooltip("Max ray distance for AboveSurfaceRaycast.")]
    public float surfaceRayMaxDistance = 200f;

    // ---------------- Playback prefs ----------------
    [Header("Playback")]
    [Tooltip("If true, keep GO active and control ParticleSystems/VFX via Play/Stop (safer for sub-emitters).")]
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
    Coroutine _playerFinderCo, _hoverFinderCo;

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
        if (_hoverFinderCo != null) StopCoroutine(_hoverFinderCo);
        _playerFinderCo = _hoverFinderCo = null;
    }

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
            Debug.LogWarning("BeaconVFXActivator: No VFX found.", this);
            return;
        }

        _particles = _vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
#if UNITY_VISUAL_EFFECT_GRAPH
        _visualEffects = _vfxInstance.GetComponentsInChildren<VisualEffect>(true);
#endif
        if (!playStopOnly) _vfxInstance.SetActive(false);
        SetVFXActive(false, immediate: true);
    }

    void Update()
    {
        if (_vfxInstance == null) return;

        // keep trying to bind
        if (player == null && searchForPlayerIfNull && _playerFinderCo == null)
            _playerFinderCo = StartCoroutine(Co_FindPlayerLoop());
       
        bool playerOK = false, hoverOK = false;

        // player trigger
        if (enablePlayerRadius && player != null)
        {
            _lastDist = Vector3.Distance(player.position, transform.position);
            playerOK = _lastDist < activationRadius;
        }

       


        //patch

        bool shouldActivate = ComputeShouldActivate(playerOK);
        if (!_isActive && shouldActivate) SetVFXActive(true);
        else if (_isActive && deactivateWhenFar && !shouldActivate) SetVFXActive(false);








        if (verbose)
        {
            int alive = 0;
            if (_particles != null) foreach (var ps in _particles) if (ps) alive += ps.particleCount;
          //  Debug.Log($"[BeaconVFX] Active={_isActive} PlayerOK={playerOK} HoverOK={hoverOK} Dist={_lastDist:F2} Alt={(_hoverLastAlt.HasValue ? _hoverLastAlt.Value : float.NaN):F2} Alive={alive}", this);
        }
    }

    bool ComputeShouldActivate(bool playerOK)
    {
        // no triggers = never
        if (!enablePlayerRadius) return false;

        if (triggerMode == TriggerMode.Any)
            return (enablePlayerRadius && playerOK) ;;

        // TriggerMode.All → only the enabled ones must be true
        bool passPlayer = !enablePlayerRadius || playerOK;
    
        return passPlayer  ;
    }



 
 

    // ---------- Play/Stop ----------
    void SetVFXActive(bool active, bool immediate = false)
    {
        _isActive = active;

        if (!playStopOnly)
            _vfxInstance.SetActive(active);

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
                else vfx.Stop();
            }
        }


     


#endif
    }

    // ---------- Finders ----------
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
        if (obj != null && obj.activeInHierarchy) player = obj.transform;
    }

 

    // public setters for runtime spawners
    public void SetPlayer(Transform t) { player = t; }
 

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

        if (enableHoverAltitude && hovercar != null)
        {
            // simple visual for AboveBeaconY mode
            if (altitudeMode == AltitudeMode.AboveBeaconY)
            {
                Gizmos.color = Color.magenta;
                Vector3 a = new Vector3(transform.position.x, transform.position.y + enterHeight, transform.position.z);
                Vector3 b = new Vector3(transform.position.x, transform.position.y + exitHeight, transform.position.z);
                Gizmos.DrawLine(transform.position, a);
                Gizmos.color = new Color(1f, 0f, 1f, 0.25f);
                Gizmos.DrawLine(transform.position, b);
            }
        }
    }
}

