using UnityEngine;
using SickscoreGames.HUDNavigationSystem;

public class HNSPlayerBinder : MonoBehaviour
{
    [Tooltip("Leave blank to auto-find HUDNavigationSystem")]
    public HUDNavigationSystem hns;

    [Tooltip("Tag to find the runtime player if you don't pass one manually")]
    public string playerTag = "Player";

 public static HNSPlayerBinder Instance { get; private set; }


    void Awake()
    {
            DontDestroyOnLoad(gameObject);
        // if (Instance && Instance != this) { Destroy(gameObject); return; }
        // Instance = this;
    
    }

    void Start()  { TryBind(null); }
    void OnEnable(){ TryBind(null); }

    // Call this from your spawner with the player Transform if you have it
    public void TryBind(Transform player)
    {
        if (!hns) hns = FindObjectOfType<HUDNavigationSystem>(true);
        if (!hns) return;

        if (!player)
        {
            var go = GameObject.FindWithTag(playerTag);
            if (go) player = go.transform;
        }
        if (!player) return;

        // Ensure markers exist (so HNS’s own auto-finder also works)
        if (!player.GetComponent<HNSPlayerController>())
            player.gameObject.AddComponent<HNSPlayerController>();

        var cam = Camera.main ? Camera.main : player.GetComponentInChildren<Camera>(true);
        if (cam && !cam.GetComponent<HNSPlayerCamera>())
            cam.gameObject.AddComponent<HNSPlayerCamera>();

        hns.PlayerController = player;
        if (cam) hns.PlayerCamera = cam;
        hns.EnableSystem(true);
    }
}
