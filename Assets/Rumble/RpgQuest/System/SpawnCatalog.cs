using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Rumble/Spawn Catalog")]
public class SpawnCatalog : ScriptableObject
{
    public AssetReferenceGameObject[] players;   // Eve, Medea, Kachujin, etc.
    public AssetReferenceGameObject[] vehicles;  // ScifiRover, RavelRover, etc.

    public int PlayerCount  => players  != null ? players.Length  : 0;
    public int VehicleCount => vehicles != null ? vehicles.Length : 0;

    public AssetReferenceGameObject GetPlayer(int index)
    {
        if (PlayerCount == 0) return null;
        index = Mathf.Clamp(index, 0, PlayerCount - 1);
        return players[index];
    }

    public AssetReferenceGameObject GetVehicle(int index)
    {
        if (VehicleCount == 0) return null;
        index = Mathf.Clamp(index, 0, VehicleCount - 1);
        return vehicles[index];
    }
}
