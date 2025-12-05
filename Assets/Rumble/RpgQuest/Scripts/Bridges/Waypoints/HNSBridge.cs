// Assets/Scripts/System/HNSBridge.cs
using UnityEngine;
using SickscoreGames.HUDNavigationSystem;

public class HNSBridge : MonoBehaviour
{
    public static HNSBridge Instance { get; private set; }

    Transform _pendingPlayer;
    Camera   _pendingCamera;
    HUDNavigationSystem _hns;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Late discovery: HUDNavigationSystem might appear later or be inactive initially
        if (_hns == null)
        {
            _hns = HUDNavigationSystem.Instance; // finds one in scene when present
            if (_hns != null && (_pendingPlayer || _pendingCamera))
                ApplyBinding();
        }
    }

    /// <summary>
    /// Call this after your async spawn completes.
    /// Camera is optional; pass null to auto-use Camera.main.
    /// </summary>
    public static void Bind(Transform player, Camera camera = null)
    {
        if (!Instance)
        {
            var go = new GameObject("HNSBridge");
            Instance = go.AddComponent<HNSBridge>();
            DontDestroyOnLoad(go);
        }

        Instance._pendingPlayer = player;
        Instance._pendingCamera = camera;
        Instance.ApplyBinding(); // try now; if HNS not ready, Update() will finish it later
    }

    void ApplyBinding()
    {
        if (_hns == null) return;
        if (_pendingPlayer == null && _pendingCamera == null) return;

        // 1) Ensure system is on (this toggles the canvas when refs are valid)
        _hns.EnableSystem(true); // will enable HUDNavigationCanvas when both refs are present. :contentReference[oaicite:2]{index=2}

        // 2) Assign camera first (so references are complete as soon as we set the player)
        var cam = _pendingCamera ? _pendingCamera : Camera.main;
        if (cam) _hns.ChangePlayerCamera(cam);  // explicit camera bind. :contentReference[oaicite:3]{index=3}

        // 3) Assign player transform
        if (_pendingPlayer) _hns.ChangePlayerController(_pendingPlayer); // explicit player bind. :contentReference[oaicite:4]{index=4}

        // 4) If either ref was missing, EnableSystem() left the canvas disabled;
        //    now that we’ve set both, call once more to guarantee canvas is up.
        _hns.EnableSystem(true); // re-checks refs and enables canvas if ready. :contentReference[oaicite:5]{index=5}

        // clear pending once applied
        _pendingPlayer = null;
        _pendingCamera = null;
    }
}
