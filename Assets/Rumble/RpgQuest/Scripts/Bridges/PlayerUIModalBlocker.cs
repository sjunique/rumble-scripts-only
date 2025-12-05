using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIModalBlocker : MonoBehaviour
{
    [Header("How to find the player clone")]
    [SerializeField] string playerTag = "Player";     // or leave blank to use type search
    [SerializeField] bool searchByInvectorController = true; // falls back if tag not found

    [Header("Cursor handling")]
    [SerializeField] bool unlockCursorWhileOpen = true;

    GameObject _player;
    int _playerId = -1;
public static PlayerUIModalBlocker Instance { get; private set; }
    readonly List<GraphicRaycaster> _raycasters = new();
    readonly List<bool> _prevEnabled = new();
    bool _active;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // Call this when your modal (shop/dialog) is shown
    public void Begin()
    {
        EnsurePlayerAndCache();
        ToggleRaycasters(false);

        if (unlockCursorWhileOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        _active = true;
    }

    // Call this when your modal closes
    public void End()
    {
        ToggleRaycasters(true);
        _active = false;
    }

    // If you know the instance at spawn time, call this once:
    public void AttachToPlayer(GameObject playerInstance)
    {
        _player = playerInstance;
        _playerId = playerInstance ? playerInstance.GetInstanceID() : -1;
        RebuildCache();
    }

    // ────────────────────────────────────────────────────────────────
    void EnsurePlayerAndCache()
    {
        if (_player && _player.GetInstanceID() == _playerId) return;

        // Try tag first
        if (!string.IsNullOrEmpty(playerTag))
            _player = GameObject.FindGameObjectWithTag(playerTag);

        // Fallback: look for Invector controller on active scene
        if ((!_player || !_player.activeInHierarchy) && searchByInvectorController)
        {
            var ctrl = FindObjectOfType<Component>(true); // placeholder, replace if you want strict type
            // If you have the type available, do:
            // var ctrl = FindObjectOfType<vThirdPersonController>(true);
            if (ctrl) _player = ctrl.gameObject;
        }

        _playerId = _player ? _player.GetInstanceID() : -1;
        RebuildCache();
    }

    void RebuildCache()
    {
        _raycasters.Clear();
        _prevEnabled.Clear();

        if (!_player) return;

        // Collect ALL GraphicRaycasters under the player (HUD, AimCanvas, StunEffectCanvas, etc.)
        _player.GetComponentsInChildren(true, _raycasters);
        foreach (var _ in _raycasters) _prevEnabled.Add(true);
    }

    void ToggleRaycasters(bool enable)
    {
        if (_raycasters.Count == 0) return;

        for (int i = 0; i < _raycasters.Count; i++)
        {
            var gr = _raycasters[i];
            if (!gr) continue;

            if (_active == false)
            {
                // first time entering: remember original state
                _prevEnabled[i] = gr.enabled;
            }

            gr.enabled = enable ? _prevEnabled[i] : false;
        }
    }
}
