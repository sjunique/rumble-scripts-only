using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class CoralSpawnOnWaterTrigger : MonoBehaviour
{


 [Header("Refs")]
  
    public CoralGroupLoader loader;                     // assign in inspector
    bool _hasSpawned;






    [Header("Addressables")]
    [SerializeField] string label = "OceanCorals";

    [Header("Parent & Placement")]
    [SerializeField] Transform parent;                 // parent for spawned corals
    [SerializeField] List<Transform> spawnPoints;      // optional specific points
    [SerializeField] Vector3 areaCenterOffset;         // center offset from this trigger
    [SerializeField] Vector3 areaSize = new Vector3(20, 0, 20);

    [Header("Trigger Options")]
    [SerializeField] string playerTag = "Player";      // your player clones use Tag "Player"
    [SerializeField] bool spawnOnce = true;            // true = only first entry spawns
    [SerializeField] bool despawnOnExit = false;       // optional cleanup when player leaves
    [SerializeField] bool randomizeRotationY = true;

    bool _spawned;
    bool _loading;
    readonly List<AsyncOperationHandle<GameObject>> _instances = new();
    AsyncOperationHandle<IList<IResourceLocation>> _locs;
    CancellationTokenSource _cts;
[SerializeField] float armDelay = 1.5f;      // seconds after scene load
[SerializeField] float waterLineY = 0f;      // set your water height
[SerializeField] float minDepth = 0.2f;      // require player below waterline by this much
float _armedAt;

void Awake(){
        _armedAt = Time.time + armDelay;
    
     if (!parent) parent = transform;
    
     }

void OnTriggerEnter(Collider other){
    if (Time.time < _armedAt) return;
    if (!other.CompareTag(playerTag)) return;
    var py = other.transform.position.y;
    if (py > waterLineY - minDepth) return;  // not deep enough → ignore
    _ = loader.SpawnAsync(new System.Threading.CancellationToken());
}
    

    
 

    void OnTriggerExit(Collider other)
    {
        if (!other || !other.CompareTag(playerTag)) return;
        if (despawnOnExit && loader != null) loader.Despawn();
        if (despawnOnExit) _hasSpawned = false;
    }

    void OnDestroy() { _cts?.Cancel(); _cts?.Dispose(); }

    async Task SpawnAllAsync(CancellationToken ct)
    {
        _loading = true;

        // Load all prefab locations under the label
        _locs = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        await _locs.Task;
        ct.ThrowIfCancellationRequested();

        if (_locs.Status != AsyncOperationStatus.Succeeded || _locs.Result == null || _locs.Result.Count == 0)
        {
            Debug.LogError($"[CoralSpawnOnWaterTrigger] No Addressable locations for label '{label}'.");
            _loading = false;
            return;
        }

        var locs = _locs.Result;
        for (int i = 0; i < locs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var pos = GetSpawnPosition(i);
            var rot = randomizeRotationY ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;

            var h = Addressables.InstantiateAsync(locs[i], pos, rot, parent);
            await h.Task;

            if (h.Status == AsyncOperationStatus.Succeeded)
            {
                _instances.Add(h);
                h.Result.name = locs[i].PrimaryKey; // keeps original asset name
            }
            else
            {
                Debug.LogError($"[CoralSpawnOnWaterTrigger] Failed to instantiate {locs[i].PrimaryKey}");
            }
        }

        _spawned = true;
        _loading = false;
        Debug.Log($"[CoralSpawnOnWaterTrigger] Spawned {_instances.Count} corals from '{label}'.");
    }

    void Cleanup()
    {
        for (int i = 0; i < _instances.Count; i++)
            if (_instances[i].IsValid()) Addressables.ReleaseInstance(_instances[i]);
        _instances.Clear();

        if (_locs.IsValid()) Addressables.Release(_locs);
        _locs = default;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
            return spawnPoints[index % spawnPoints.Count].position;

        var center = transform.TransformPoint(areaCenterOffset);
        var half = areaSize * 0.5f;
        var rand = new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z)
        );
        return center + rand;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.15f);
        Gizmos.DrawCube(areaCenterOffset, areaSize);
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.9f);
        Gizmos.DrawWireCube(areaCenterOffset, areaSize);
    }
#endif
}
