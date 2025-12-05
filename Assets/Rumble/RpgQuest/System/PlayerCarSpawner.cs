using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;

public class PlayerCarSpawner : MonoBehaviour
{
    public static event Action<GameObject> OnPlayerSpawned;

    [Header("Optional explicit refs (can be left empty)")]
    public SelectedLoadout selectedLoadout; // leave null -> will use SelectedLoadout.Instance

    public LoadoutCatalog catalog; // leave null -> will use SelectedLoadout.Catalog

    [Header("Spawn points")] public Transform playerSpawn;
    public Transform carSpawn;

    private GameObject _player, _car;

    async void Start()
    {
        var lo = selectedLoadout ? selectedLoadout : SelectedLoadout.Instance;
        if (!lo)
        {
            Debug.LogError("[Spawner] SelectedLoadout missing.");
            return;
        }

        var cat = catalog ? catalog : lo.Catalog;
        if (!cat)
        {
            Debug.LogError("[Spawner] LoadoutCatalog missing.");
            return;
        }

        // --- wait up to 2s for IDs to be set & resolvable
        float t = 0f;
        while (t < 2f && (string.IsNullOrEmpty(lo.CharacterId)
                          || string.IsNullOrEmpty(lo.VehicleId)
                          || cat.GetCharacter(lo.CharacterId) == null
                          || cat.GetVehicle(lo.VehicleId) == null))
        {
            await System.Threading.Tasks.Task.Yield();
            t += Time.deltaTime;
        }

        // --- resolve local, safe IDs (no assigning back to SelectedLoadout)
        string charId = !string.IsNullOrEmpty(lo.CharacterId) ? lo.CharacterId
            : (cat.characters != null && cat.characters.Length > 0) ? cat.characters[0]?.id : null;

        string vehId = !string.IsNullOrEmpty(lo.VehicleId) ? lo.VehicleId
            : (cat.vehicles != null && cat.vehicles.Length > 0) ? cat.vehicles[0]?.id : null;

        var charDef = string.IsNullOrEmpty(charId) ? null : cat.GetCharacter(charId);
        var vehDef = string.IsNullOrEmpty(vehId) ? null : cat.GetVehicle(vehId);

        if (charDef == null)
        {
            Debug.LogError($"[Spawner] Character id '{charId}' not found.");
            return;
        }

        if (vehDef == null)
        {
            Debug.LogError($"[Spawner] Vehicle id '{vehId}' not found.");
            return;
        }

        // --- instantiate via AssetReferenceGameObject RuntimeRef
        if (charDef.RuntimeRef != null && charDef.RuntimeRef.RuntimeKeyIsValid())
        {
            var hc = charDef.RuntimeRef.InstantiateAsync(playerSpawn);
            await hc.Task;
            if (hc.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                _player = hc.Result;
            else Debug.LogError($"[Spawner] Addressables failed for character '{charId}'.");

            GameObject clone = hc.Result;

            // 1) Resolve respawn fields on your scene respawner
            var sceneRespawn = FindObjectOfType<SimpleRespawn>();
            if (sceneRespawn != null) sceneRespawn.ResolveForClone(clone);

            var autoWire = clone.GetComponentInChildren<SimpleRespawnAutoWire>(true);
            if (autoWire == null)
            {
                // add the component to the clone root so it can access children
                autoWire = clone.AddComponent<SimpleRespawnAutoWire>();
                // ensure it won't attempt to auto-find a respawn on the clone
                autoWire.autoFindRespawn = false;
            }

            // call AutoWireFromClone so the clone's health is located and pushed into the scene respawn
            autoWire.AutoWireFromClone(clone, sceneRespawn);
            autoWire.WireEvents(sceneRespawn);
            Debug.Log("[Spawner] Auto-wired SimpleRespawnAutoWire on player clone.");
        }
        else Debug.LogError($"[Spawner] ActorDef '{charDef.name}' missing RuntimeRef.");

        if (vehDef.RuntimeRef != null && vehDef.RuntimeRef.RuntimeKeyIsValid())
        {
            var hv = vehDef.RuntimeRef.InstantiateAsync(carSpawn);
            await hv.Task;
            if (hv.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                _car = hv.Result;
            else Debug.LogError($"[Spawner] Addressables failed for vehicle '{vehId}'.");
        }
        else Debug.LogError($"[Spawner] ActorDef '{vehDef.name}' missing RuntimeRef.");

        // Optional: bind HUD/etc here
        if (_player)
        {
            HNSBridge.Bind(_player.transform);
            OnPlayerSpawned?.Invoke(_player); // <— notify listeners exactly once
        }


        //HNSBridge.Bind(_player.transform);
    }

    public void Despawn()
    {
        if (_player) Addressables.ReleaseInstance(_player);
        if (_car) Addressables.ReleaseInstance(_car);
        _player = _car = null;
    }
}