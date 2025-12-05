 

using UnityEngine;

public static class InputContextFixer
{
    // Try a few common gameplay maps so you don't have to rename anything now.
    static readonly string[] GameplayMaps = { "Gameplay", "Player", "Default", "Game" };

    public static void EnsureGameplayMap(GameObject player)
    {
        if (!player) return;

#if ENABLE_INPUT_SYSTEM
        var pi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi)
        {
            var current = pi.currentActionMap?.name ?? "(none)";
            // If we're already on a non-UI map, leave it.
            if (!string.Equals(current, "UI", System.StringComparison.OrdinalIgnoreCase))
                return;

            foreach (var map in GameplayMaps)
            {
                var am = pi.actions.FindActionMap(map, true);
                if (am != null)
                {
                    pi.SwitchCurrentActionMap(map);
                    Debug.Log($"[InputContextFixer] Switched PlayerInput map: UI → {map}");
                    return;
                }
            }

            // Fallback: first non-UI map
            foreach (var am in pi.actions.actionMaps)
            {
                if (!string.Equals(am.name, "UI", System.StringComparison.OrdinalIgnoreCase))
                {
                    pi.SwitchCurrentActionMap(am.name);
                    Debug.Log($"[InputContextFixer] Switched PlayerInput map: UI → {am.name} (fallback)");
                    return;
                }
            }

            Debug.LogWarning("[InputContextFixer] Could not find a non-UI action map to switch to.");
        }
#endif
    }
}
