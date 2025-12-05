// Assets/Rumble/RpgQuest/Bridges/Waypoints/SimpleRouteRevealer.cs
using UnityEngine;

public class SimpleRouteRevealer : MonoBehaviour
{
    [Header("Stage objects")]
    public GameObject routeRoot;        // parent with PathManager + visual(s) (inactive at start)
    public GameObject collectiblesRoot; // your pickups parent (inactive at start)

    [Header("Optional: ensure controls stay on")]
    public Transform player;

    public void OnAccept()
    {
        if (routeRoot) routeRoot.SetActive(true);
        if (collectiblesRoot) collectiblesRoot.SetActive(true);

        // (safety) make sure WASD is enabled
        if (player)
        {
            var input = FindComp(player, "vThirdPersonInput") as Behaviour;
            if (input) input.enabled = true;

            var ctrl = FindComp(player, "vThirdPersonController");
            var prop = ctrl ? ctrl.GetType().GetProperty("lockMovement") : null;
            if (prop != null && prop.PropertyType == typeof(bool)) prop.SetValue(ctrl, false, null);
        }

        Debug.Log("[SimpleRoute] Revealed route & collectibles.");
    }

    Component FindComp(Transform root, string typeName)
    {
        if (!root) return null;
        var all = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in all)
        {
            var t = c ? c.GetType() : null;
            if (t == null) continue;
            if (t.Name == typeName || t.FullName.EndsWith("." + typeName)) return c;
        }
        return null;
    }
}
