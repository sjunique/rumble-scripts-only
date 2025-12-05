using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CoralZoneLoader : MonoBehaviour
{
    [Header("Either set a label, or fill explicit addresses")]
    public string coralLabel = ""; // e.g., "coral" if you created that label
    public List<string> coralAddresses = new() { "WhisperingReef", "RainbowReef", "FishBoid", "Aurora" };

    public Transform coralParent;       // e.g., your OceanCorals transform
    public int spawnCount = 6;          // how many instances to drop
    public Vector3 scatter = new(30, 0, 30);
    public Vector3 baseOffset = new(0, -5, 0);

    private readonly List<GameObject> _spawned = new();
    private bool _busy;
    private AsyncOperationHandle<IList<GameObject>> _catalogHandle;
    private bool _usingLabel;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!_busy) _ = LoadAndSpawn();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        DespawnAll();
        if (_usingLabel && _catalogHandle.IsValid())
        {
            Addressables.Release(_catalogHandle);
            _catalogHandle = default;
        }
    }

    private async Task LoadAndSpawn()
    {
        _busy = true;

        IList<GameObject> sourceList = null;
        _usingLabel = !string.IsNullOrEmpty(coralLabel);

        if (_usingLabel)
        {
            _catalogHandle = Addressables.LoadAssetsAsync<GameObject>(coralLabel, null);
            await _catalogHandle.Task;
            if (_catalogHandle.Status == AsyncOperationStatus.Succeeded)
                sourceList = _catalogHandle.Result;
        }
        else
        {
            // load each explicit address once to sample from
            var temp = new List<GameObject>();
            foreach (var key in coralAddresses)
            {
                var h = Addressables.LoadAssetAsync<GameObject>(key);
                await h.Task;
                if (h.Status == AsyncOperationStatus.Succeeded && h.Result) temp.Add(h.Result);
                Addressables.Release(h); // we only needed the asset to pick a prefab name
            }
            sourceList = temp;
        }

        if (sourceList == null || sourceList.Count == 0) { _busy = false; return; }

        for (int i = 0; i < spawnCount; i++)
        {
            var prefab = sourceList[Random.Range(0, sourceList.Count)];
            var go = await SceneLoadService.Instance.InstantiateAsync(prefab.name, coralParent);
            var rnd = new Vector3(Random.Range(-scatter.x, scatter.x), Random.Range(-scatter.y, scatter.y), Random.Range(-scatter.z, scatter.z));
            go.transform.localPosition = baseOffset + rnd;
            _spawned.Add(go);
        }

        _busy = false;
    }

    private void DespawnAll()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) SceneLoadService.Instance.ReleaseInstance(_spawned[i]);
        _spawned.Clear();
    }
}
