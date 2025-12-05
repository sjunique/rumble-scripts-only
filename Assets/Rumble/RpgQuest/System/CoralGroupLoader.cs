using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class CoralGroupLoader : MonoBehaviour
{
      [Header("Addressables")]
    [SerializeField] string label = "OceanCorals";
    [SerializeField] bool autoStart = false;          // NEW

    [Header("Parent & Placement")]
    [SerializeField] Transform parent;
    [SerializeField] List<Transform> spawnPoints;
    [SerializeField] Vector3 areaCenter;
    [SerializeField] Vector3 areaSize = new Vector3(20, 0, 20);
    [SerializeField] bool randomizeRotationY = true;

    readonly List<AsyncOperationHandle<GameObject>> _instanceHandles = new();
    AsyncOperationHandle<IList<IResourceLocation>> _locationsHandle;
    CancellationTokenSource _cts;
    bool _spawned, _spawning;
   

 void Awake() { if (!parent) parent = transform; }

    async void Start()
    {
        if (autoStart) { _cts = new(); await SpawnAsync(_cts.Token); }
    }

    void OnDestroy() { Despawn(); _cts?.Cancel(); _cts?.Dispose(); }


 public async Task SpawnAsync(CancellationToken ct)                // NEW (public)
    {
        if (_spawning || _spawned) return;
        _spawning = true;

        _locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        await _locationsHandle.Task;
        ct.ThrowIfCancellationRequested();

        if (_locationsHandle.Status != AsyncOperationStatus.Succeeded || _locationsHandle.Result == null || _locationsHandle.Result.Count == 0)
        { Debug.LogError($"[CoralGroupLoader] No locations for label '{label}'."); _spawning = false; return; }

        var locs = _locationsHandle.Result;
        for (int i = 0; i < locs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var pos = GetSpawnPosition(i);
            var rot = randomizeRotationY ? Quaternion.Euler(0, Random.Range(0f, 360f), 0) : Quaternion.identity;

            var h = Addressables.InstantiateAsync(locs[i], pos, rot, parent);
            await h.Task;
            if (h.Status == AsyncOperationStatus.Succeeded)
            { _instanceHandles.Add(h); h.Result.name = locs[i].PrimaryKey; }
        }

        _spawned = true;
        _spawning = false;
    }


  public void Despawn()                                             // NEW (public)
    {
        for (int i = 0; i < _instanceHandles.Count; i++)
            if (_instanceHandles[i].IsValid()) Addressables.ReleaseInstance(_instanceHandles[i]);
        _instanceHandles.Clear();

        if (_locationsHandle.IsValid()) Addressables.Release(_locationsHandle);
        _locationsHandle = default;
        _spawned = false;
        _spawning = false;
    }
    Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
            return spawnPoints[index % spawnPoints.Count].position;

        var half = areaSize * 0.5f;
        var rand = new Vector3(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y), Random.Range(-half.z, half.z));
        return areaCenter + rand;
    }


    public async Task LoadAndSpawnByLabelAsync(string addressableLabel, CancellationToken ct)
    {
        // 1) Get all prefab locations under the label
        _locationsHandle = Addressables.LoadResourceLocationsAsync(addressableLabel, typeof(GameObject));
        await _locationsHandle.Task;
        ct.ThrowIfCancellationRequested();

        if (_locationsHandle.Status != AsyncOperationStatus.Succeeded || _locationsHandle.Result == null || _locationsHandle.Result.Count == 0)
        {
            Debug.LogError($"[CoralGroupLoader] No locations found for label '{addressableLabel}'.");
            return;
        }

        var locations = _locationsHandle.Result;

        // 2) Instantiate each prefab
        for (int i = 0; i < locations.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var pos = GetSpawnPosition(i);
            var rot = randomizeRotationY ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;

            var instHandle = Addressables.InstantiateAsync(locations[i], pos, rot, parent);
            await instHandle.Task;

            if (instHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _instanceHandles.Add(instHandle);

                // Optional: tidy up the name (Unity often adds “(Clone)”)
                instHandle.Result.name = locations[i].PrimaryKey; // keeps original asset name (e.g., "whisperingreef (1)")
            }
            else
            {
                Debug.LogError($"[CoralGroupLoader] Failed to instantiate: {locations[i].PrimaryKey}");
            }
        }

        Debug.Log($"[CoralGroupLoader] Spawned {_instanceHandles.Count} coral prefabs from label '{addressableLabel}'.");
    }

    // Vector3 GetSpawnPosition(int index)
    // {
    //     if (spawnPoints != null && spawnPoints.Count > 0)
    //         return spawnPoints[index % spawnPoints.Count].position;

    //     // Random point in a flat box (XZ)
    //     var half = areaSize * 0.5f;
    //     var rand = new Vector3(
    //         Random.Range(-half.x, half.x),
    //         Random.Range(-half.y, half.y),
    //         Random.Range(-half.z, half.z)
    //     );
    //     return areaCenter + rand;
    // }
}
