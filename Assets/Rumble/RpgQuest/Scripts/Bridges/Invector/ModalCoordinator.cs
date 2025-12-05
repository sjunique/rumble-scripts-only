using UnityEngine;
using UnityEngine.EventSystems;

public class ModalCoordinator : MonoBehaviour
{
    static ModalCoordinator _i;
    public static ModalCoordinator I {
        get {
            if (_i) return _i;
            var go = new GameObject("~ModalCoordinator"); DontDestroyOnLoad(go);
            _i = go.AddComponent<ModalCoordinator>();
            return _i;
        }
    }

    [SerializeField] GameObject player; // optional (auto-find by tag)
    [SerializeField] string playerTag = "Player";

    int depth;
    bool invectorWasEnabled;
    Behaviour invectorInput;

    void EnsurePlayer()
    {
        if (player) return;
        if (!string.IsNullOrEmpty(playerTag))
            player = GameObject.FindGameObjectWithTag(playerTag);
        if (player) invectorInput = player.GetComponent("vThirdPersonInput") as Behaviour;
    }

    public bool IsOpen => depth > 0;

    public void Open()
    {
        depth++;
        if (depth > 1) { // already modal
            ForceCursor(true);
            return;
        }

        // 1) suspend uGUI ES's (your orchestrator)
        EventSystemOrchestrator.I?.SuspendAllUGUI();

        // 2) mute shooter HUD raycasters (optional if you already suspend ES’s)
        FindObjectOfType<PlayerUIModalBlocker>()?.Begin();

        // 3) freeze player & cursor
        EnsurePlayer();
        if (invectorInput) { invectorWasEnabled = invectorInput.enabled; invectorInput.enabled = false; }

        ForceCursor(true);
    }

    public void Close()
    {
        if (depth == 0) return;
        depth--;
        if (depth > 0) { ForceCursor(true); return; }

        // last modal closed → restore world
        FindObjectOfType<PlayerUIModalBlocker>()?.End();
        EventSystemOrchestrator.I?.ResumeAllUGUI();

        if (invectorInput) invectorInput.enabled = invectorWasEnabled;

        ForceCursor(false);
    }

    void ForceCursor(bool on)
    {
        Cursor.visible = on;
        Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
