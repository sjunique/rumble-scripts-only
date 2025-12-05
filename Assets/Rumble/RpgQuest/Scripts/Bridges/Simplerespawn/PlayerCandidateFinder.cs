using System.Linq;
using UnityEngine;

public static class PlayerCandidateFinder
{
    public static GameObject FindBestPlayerCandidate()
    {
        // 1) prefer tag "Player" if it looks real
        var tagged = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
        if (IsRealPlayerCandidate(tagged)) { Debug.Log("[Finder] Selected by tag: "+tagged.name); return tagged; }

        // 2) prefer objects with vThirdPersonController
        var controllers = GameObject.FindObjectsOfType(typeof(Invector.vCharacterController.vThirdPersonController), true)
                                   .Cast<Invector.vCharacterController.vThirdPersonController>()
                                   .Where(c => c != null).ToArray();
        if (controllers.Length > 0)
        {
            foreach (var c in controllers)
            {
                var root = c.transform.root.gameObject;
                if (root.GetComponent<SimpleRespawn>() != null) continue;
                if (!IsRealPlayerCandidate(root)) continue;
                Debug.Log("[Finder] Selected by controller: "+root.name);
                return root;
            }
            Debug.Log("[Finder] Fallback controller root: "+controllers[0].transform.root.name);
            return controllers[0].transform.root.gameObject;
        }

        // 3) objects with vHealthController
        var healths = GameObject.FindObjectsOfType(typeof(Invector.vHealthController), true)
                                .Cast<Invector.vHealthController>().Where(h => h != null).ToArray();
        if (healths.Length > 0) { var root = healths[0].transform.root.gameObject; if (IsRealPlayerCandidate(root)) { Debug.Log("[Finder] Selected by health: "+root.name); return root; } }

        // 4) fallback: Camera.main root or named player
        if (Camera.main != null) { var camRoot = Camera.main.transform.root.gameObject; if (IsRealPlayerCandidate(camRoot)) { Debug.Log("[Finder] Selected by Camera.main root: "+camRoot.name); return camRoot; } }
        var named = GameObject.Find("Player") ?? GameObject.Find("PlayerRoot");
        if (IsRealPlayerCandidate(named)) { Debug.Log("[Finder] Selected by name: "+named.name); return named; }

        Debug.LogWarning("[Finder] No good player candidate found.");
        return null;
    }

    static bool IsRealPlayerCandidate(GameObject go)
    {
        if (go == null) return false;
        if (go.GetComponent<SimpleRespawn>() != null) return false;
        bool hasController = go.GetComponentInChildren<Invector.vCharacterController.vThirdPersonController>(true) != null;
        bool hasHealth = go.GetComponentInChildren<Invector.vHealthController>(true) != null;
        bool hasInput = go.GetComponentInChildren<Invector.vCharacterController.vThirdPersonInput>(true) != null;
        return hasController || hasHealth || hasInput;
    }
}

