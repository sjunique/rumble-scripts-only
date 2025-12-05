using UnityEngine;
 
public class SelectedLoadout : MonoBehaviour
{
    public static SelectedLoadout Instance { get; private set; }

    [Header("Catalog (assign in Selection scene)")]
   // inside SelectedLoadout
[SerializeField] LoadoutCatalog catalog;
 

    [Header("Chosen IDs")]
    [SerializeField] string characterId;
    [SerializeField] string vehicleId;

    public enum Slot { Character, Vehicle }

// SelectedLoadout.cs
public static bool LaunchedFromLevelSelect { get; private set; }
public static void MarkLevelSelectLaunch() => LaunchedFromLevelSelect = true;
public static void ClearLevelSelectLaunch() => LaunchedFromLevelSelect = false;



// inside SelectedLoadout
 
public LoadoutCatalog Catalog => catalog;

 // SelectedLoadout.cs (in Bootstrap)
void Awake()
{
    if (Instance && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    // Seed defaults if empty
    if (Catalog != null)
    {
        if (string.IsNullOrEmpty(characterId) && Catalog.characters != null && Catalog.characters.Length > 0)
            characterId = Catalog.characters[0]?.id;

        if (string.IsNullOrEmpty(vehicleId) && Catalog.vehicles != null && Catalog.vehicles.Length > 0)
            vehicleId = Catalog.vehicles[0]?.id;
    }
}


    // ---- Setters used by Selection ----
    public void SetCharacter(string id) => characterId = id;
    public void SetVehicle(string id)   => vehicleId = id;

    // Back-compat shim (if older code still calls Set(slot, prefab))
    public void Set(Slot slot, GameObject prefab)
    {
        if (!catalog) return;
        if (slot == Slot.Character)
        {
            var def = System.Array.Find(catalog.characters, d => d && (d.previewPrefab == prefab || d.runtimePrefab == prefab));
            if (def) characterId = def.id;
        }
        else
        {
            var def = System.Array.Find(catalog.vehicles, d => d && (d.previewPrefab == prefab || d.runtimePrefab == prefab));
            if (def) vehicleId = def.id;
        }

//Debug.Log($"[Loadout] CharacterId set: {id}").Log();
        
    }

    // ---- Resolve to prefabs when needed ----
    public GameObject GetPreviewCharacter() => catalog?.GetCharacter(characterId)?.previewPrefab;
    public GameObject GetPreviewVehicle()   => catalog?.GetVehicle(vehicleId)?.previewPrefab;
    public GameObject GetRuntimeCharacter() => catalog?.GetCharacter(characterId)?.runtimePrefab;
    public GameObject GetRuntimeVehicle()   => catalog?.GetVehicle(vehicleId)?.runtimePrefab;

    // Optional convenience for gameplay spawners already in your code:
    public GameObject Spawn(Slot slot, Transform t)
    {
        var prefab = (slot == Slot.Character) ? GetRuntimeCharacter() : GetRuntimeVehicle();
        if (!prefab) return null;
        return t ? Instantiate(prefab, t.position, t.rotation) : Instantiate(prefab);
    }

    public string CharacterId => characterId;
    public string VehicleId   => vehicleId;
}
