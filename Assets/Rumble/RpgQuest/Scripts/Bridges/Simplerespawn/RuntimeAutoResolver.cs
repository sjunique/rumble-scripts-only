 

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class RuntimeAutoResolver : MonoBehaviour
{
    [Tooltip("Target player GameObject to repair references on. Leave null to auto-find by tag 'Player' or Camera.main parent.")]
    public GameObject targetPlayer;

    [Tooltip("Run automatically on Start (waits one frame then runs).")]
    public bool runOnStart = true;

    [Tooltip("If true, will try to resolve all UnityEngine.Object fields on matching components.")]
    public bool autoResolveAll = true;

    [Tooltip("Types (full name) to attempt automatic resolution on. Default includes Invector melee components.")]
    public string[] typesToResolve = new string[]
    {
        "Invector.vMelee.vMeleeAttackObject",
        "Invector.vMelee.vHitBox",
        "Invector.vMelee.vMeleeWeapon",
        "Invector.vShooter.vShooterManager", // optional
        "Invector.vCharacterController.vThirdPersonController",
        "Invector.vItemManager", // optional
    };

    // how many seconds to attempt re-resolve retries (in case spawner attaches things later)
    public float retryDuration = 2f;

    void Start()
    {
        if (runOnStart)
            StartCoroutine(RunResolveCoroutine());
    }

    public void RunNow()
    {
        StartCoroutine(RunResolveCoroutine());
    }

    IEnumerator RunResolveCoroutine()
    {
        // wait one frame so prefab Awakes/Starts run
        yield return null;

        float start = Time.realtimeSinceStartup;
        bool anyResolved = false;
        do
        {
            var player = GetTargetPlayer();
            if (player == null)
            {
                Debug.LogWarning("[AutoResolver] Target player not found. Will retry briefly.");
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // run resolution pass
            bool passResolved = ResolveOnPlayer(player);
            anyResolved |= passResolved;

            if (passResolved)
            {
                // do one more pass to be sure indirect references resolved too
                yield return null;
                ResolveOnPlayer(player);
                break;
            }

            yield return new WaitForSeconds(0.1f);
        } while (Time.realtimeSinceStartup - start < retryDuration);

        if (!anyResolved)
            Debug.LogWarning("[AutoResolver] No references were auto-resolved. Check logs for details.");
        else
            Debug.Log("[AutoResolver] Auto-resolve complete.");
    }






    private GameObject GetTargetPlayer()
    {
        if (targetPlayer != null) return targetPlayer;

        // try find tag "Player"
        var byTag = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
        if (byTag != null) return byTag;

        // fallback: if main camera exists, prefer its root or parent with 'Player' in name
        if (Camera.main != null)
        {
            var camRoot = Camera.main.transform.root.gameObject;
            // heuristics
            if (camRoot.name.ToLower().Contains("player") || camRoot.GetComponentInChildren<Component>() != null)
                return camRoot;
        }

        // last resort: find first object that has any of our target types
        foreach (var name in typesToResolve)
        {
            var t = GetTypeByName(name);
            if (t == null) continue;
            var found = FindObjectOfType(t, true) as Component;
            if (found != null)
                return found.transform.root.gameObject;
        }

        return null;
    }

    private bool ResolveOnPlayer(GameObject player)
    {
        bool anyResolvedThisPass = false;

        foreach (var typeName in typesToResolve)
        {
            var t = GetTypeByName(typeName);
            if (t == null) continue;

            var comps = player.GetComponentsInChildren(t, true).Cast<Component>().ToArray();
            if (comps.Length == 0) continue;

            foreach (var comp in comps)
            {
                try
                {
                    var resolved = ResolveComponentReferences(comp, player);
                    anyResolvedThisPass |= resolved;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AutoResolver] Exception resolving component {comp.GetType().Name}: {ex}");
                }
            }
        }

        // optionally run a broad sweep for other components (if autoResolveAll)
        if (autoResolveAll)
        {
            // example: resolve any UnityEngine.Object field left null on common invector components we saw
            var extraTypes = new Type[] {
                GetTypeByName("Invector.vMelee.vMeleeAttackObject"),
                GetTypeByName("Invector.vMelee.vHitBox")
            }.Where(x => x != null).ToArray();

            foreach (var t in extraTypes)
            {
                var comps = player.GetComponentsInChildren(t, true).Cast<Component>().ToArray();
                foreach (var comp in comps)
                {
                    anyResolvedThisPass |= ResolveComponentReferences(comp, player);
                }
            }
        }

        return anyResolvedThisPass;
    }

    private bool ResolveComponentReferences(Component comp, GameObject playerRoot)
    {
        bool anyAssigned = false;
        var type = comp.GetType();

        // Fields
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(f => typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType));

        foreach (var f in fields)
        {
            try
            {
                var val = f.GetValue(comp) as UnityEngine.Object;
                if (val != null) continue; // already set

                var assigned = FindBestMatchForType(f.FieldType, comp, playerRoot);
                if (assigned != null)
                {
                    f.SetValue(comp, assigned);
                    Debug.Log($"[AutoResolver] Assigned field '{f.Name}' on '{comp.GetType().Name}' -> {assigned.name}");
                    anyAssigned = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AutoResolver] Field assign failed for {comp.GetType().Name}.{f.Name}: {ex.Message}");
            }
        }

        // Properties
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(p => p.CanWrite && typeof(UnityEngine.Object).IsAssignableFrom(p.PropertyType));

        foreach (var p in props)
        {
            try
            {
                var val = p.GetValue(comp, null) as UnityEngine.Object;
                if (val != null) continue;

                var assigned = FindBestMatchForType(p.PropertyType, comp, playerRoot);
                if (assigned != null)
                {
                    p.SetValue(comp, assigned, null);
                    Debug.Log($"[AutoResolver] Assigned property '{p.Name}' on '{comp.GetType().Name}' -> {assigned.name}");
                    anyAssigned = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AutoResolver] Property assign failed for {comp.GetType().Name}.{p.Name}: {ex.Message}");
            }
        }

        return anyAssigned;
    }

    private UnityEngine.Object FindBestMatchForType(Type fieldType, Component contextComp, GameObject playerRoot)
    {
        // 1) if fieldType is GameObject
        if (fieldType == typeof(GameObject))
        {
            // try child with name matching context object, else root
            return playerRoot;
        }

        // 2) if fieldType is Transform
        if (fieldType == typeof(Transform))
        {
            return contextComp.transform;
        }

        // 3) if fieldType is a Component type, try to find in children of playerRoot
        if (typeof(Component).IsAssignableFrom(fieldType))
        {
            // prefer same object (sibling/child) first: search local hierarchy of comp
            var localMatch = contextComp.GetComponentInChildren(fieldType, true) as Component;
            if (localMatch != null) return localMatch;

            // then search playerRoot children for first component of that type
            var globalMatch = playerRoot.GetComponentInChildren(fieldType, true) as Component;
            if (globalMatch != null) return globalMatch;
        }

        // 4) asset-like types (ScriptableObject, Material, etc) - cannot guess, return null
        return null;
    }

    private Type GetTypeByName(string fullName)
    {
        // search loaded assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }


// call this when choosing the camera target instead of naive heuristics
private GameObject FindBestPlayerCandidate()
{
    // 1) explicit target if set
    if (targetPlayer != null) return targetPlayer;

    // 2) prefer objects that have vThirdPersonController
    var controllers = FindObjectsOfType<Invector.vCharacterController.vThirdPersonController>(true);
    if (controllers != null && controllers.Length > 0)
    {
        // pick the one that is active and not the respawner object
        foreach (var c in controllers)
        {
            if (c == null) continue;
            var root = c.transform.root.gameObject;
            // avoid assigning to objects named "SimpleRespawn" or that lack Health
            if (root.GetComponent<SimpleRespawn>() != null) continue;
            if (root.GetComponentInChildren<Invector.vHealthController>(true) == null) continue;
            return root;
        }
        // fallback: first controller's root
        return controllers[0].transform.root.gameObject;
    }

    // 3) fallback earlier heuristics
    var byTag = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
    if (byTag != null) return byTag;

    // last resort: camera.main's root
    if (Camera.main != null) return Camera.main.transform.root.gameObject;

    return null;
}




}
