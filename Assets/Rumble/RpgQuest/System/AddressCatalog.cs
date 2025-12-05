using UnityEngine;

[CreateAssetMenu(menuName = "Rumble/Address Catalog")]
public class AddressCatalog : ScriptableObject
{
    [Header("Addressables addresses (exactly as in Groups)")]
    public string[] playerAddresses;   // e.g. Medea, Kachujin, Jennifer, Eve, C_Knight, C_Ganfaul
    public string[] vehicleAddresses;  // e.g. ScifiRover, RavelRover, MotoRover, CrawfishRover, Bumblebee

    public int PlayerCount  => playerAddresses  != null ? playerAddresses.Length  : 0;
    public int VehicleCount => vehicleAddresses != null ? vehicleAddresses.Length : 0;

    public string PlayerByIndex(int i)
    {
        if (PlayerCount == 0) return null;
        i = Mathf.Clamp(i, 0, PlayerCount - 1);
        return playerAddresses[i];
    }
    public string VehicleByIndex(int i)
    {
        if (VehicleCount == 0) return null;
        i = Mathf.Clamp(i, 0, VehicleCount - 1);
        return vehicleAddresses[i];
    }
}
