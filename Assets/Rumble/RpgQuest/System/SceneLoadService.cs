using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public sealed class SceneLoadService : MonoBehaviour
{
    public static SceneLoadService Instance { get; private set; }

    // Track non-scene handles (instantiates, loads, etc.) so we can release on destroy
    private readonly HashSet<AsyncOperationHandle> _handles = new();

    // Track loaded scenes by key -> SceneInstance  (NOT the handle)
    private readonly Dictionary<string, SceneInstance> _loadedScenes = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task InitializeAsync()
    {
        var init = Addressables.InitializeAsync();
        _handles.Add(init);
        await init.Task;
    }

    public bool IsLoaded(string key) => _loadedScenes.ContainsKey(key);

    // ----------------- Addressables Instantiate helpers -----------------

    public async Task<GameObject> InstantiateAsync(string key, Transform parent = null)
    {
        var h = Addressables.InstantiateAsync(key, parent);
        _handles.Add(h);
        await h.Task;

        if (h.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[Loader] Instantiate failed for '{key}'.");
            if (h.IsValid()) Addressables.Release(h);
            _handles.Remove(h);
            return null;
        }
        return h.Result;
    }

    public async Task<GameObject> InstantiateAsync(AssetReferenceGameObject prefabRef, Transform parent = null)
    {
        var h = prefabRef.InstantiateAsync(parent);
        _handles.Add(h);
        await h.Task;

        if (h.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[Loader] Instantiate failed for reference '{prefabRef.RuntimeKey}'.");
            if (h.IsValid()) Addressables.Release(h);
            _handles.Remove(h);
            return null;
        }
        return h.Result;
    }

    public void ReleaseInstance(GameObject go) => Addressables.ReleaseInstance(go);

    // ----------------- Scene loading / unloading -----------------

    /// <summary>Load an Addressables scene additively and (optionally) set it active.</summary>
    public async Task<SceneInstance?> LoadSceneAdditive(string key, bool activate = true)
    {
        if (string.IsNullOrEmpty(key)) return null;

        // Already loaded? just (optionally) set active and return the existing SceneInstance.
        if (_loadedScenes.TryGetValue(key, out var existing))
        {
            if (activate && existing.Scene.IsValid())
                SceneManager.SetActiveScene(existing.Scene);
            return existing;
        }

        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(key, LoadSceneMode.Additive, activate);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[Loader] Failed to load scene '{key}'. Status={handle.Status}");
            return null;
        }

        SceneInstance inst = handle.Result;
        _loadedScenes[key] = inst;

        if (activate && inst.Scene.IsValid())
            SceneManager.SetActiveScene(inst.Scene);

        return inst;
    }

    /// <summary>Unload a scene by key (Addressables if we know it; fallback to SceneManager otherwise).</summary>
    public async Task UnloadScene(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        // Preferred: use Addressables when we have the SceneInstance
        if (_loadedScenes.TryGetValue(key, out var inst))
        {
            var unloadHandle = Addressables.UnloadSceneAsync(inst, true);
            await unloadHandle.Task;
            _loadedScenes.Remove(key);
            return;
        }

        // Fallback: unload by name if it isn't tracked (e.g., loaded outside Addressables)
        Scene s = SceneManager.GetSceneByName(key);
        if (s.IsValid() && s.isLoaded)
        {
            AsyncOperation op = SceneManager.UnloadSceneAsync(s);
            while (op != null && !op.isDone) await Task.Yield();
        }
    }

    public async Task UnloadScenes(params string[] keys)
    {
        if (keys == null) return;
        foreach (var k in keys) await UnloadScene(k);
    }

    /// <summary>Load one scene and unload any number of others. Newly loaded becomes active.</summary>
    public async Task SwitchTo(string loadKey, params string[] unloadKeys)
    {
        var si = await LoadSceneAdditive(loadKey, activate: true);
        if (unloadKeys != null && unloadKeys.Length > 0)
            await UnloadScenes(unloadKeys);

        if (si.HasValue && si.Value.Scene.IsValid())
            SceneManager.SetActiveScene(si.Value.Scene);
        else
            EnsureAnyActiveScene();
    }

    public void EnsureAnyActiveScene()
    {
        var active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.IsValid() && s.isLoaded) { SceneManager.SetActiveScene(s); break; }
            }
        }
    }

    public void DumpLoaded() =>
        Debug.Log($"[Loader] Loaded: {string.Join(", ", _loadedScenes.Keys)}");

    void OnDestroy()
    {
        // Try to unload all known scenes
        foreach (var kv in new List<string>(_loadedScenes.Keys))
            _ = Addressables.UnloadSceneAsync(_loadedScenes[kv], true);

        // Release non-scene handles
        foreach (var h in _handles)
            if (h.IsValid()) Addressables.Release(h);
        _handles.Clear();

        _loadedScenes.Clear();
    }
}
