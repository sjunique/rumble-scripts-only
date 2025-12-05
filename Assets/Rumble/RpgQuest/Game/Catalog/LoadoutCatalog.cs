using UnityEngine;
using System.Linq;

// [CreateAssetMenu(menuName = "Game/Loadout Catalog")]
// public class LoadoutCatalog : ScriptableObject
// {
//     public ActorDef[] characters;
//     public ActorDef[] vehicles;

//     public ActorDef GetCharacter(string id) => characters.FirstOrDefault(x => x && x.id == id);
//     public ActorDef GetVehicle(string id)   => vehicles.FirstOrDefault(x => x && x.id == id);

//     public ActorDef GetCharacterByIndex(int i) => (i >= 0 && i < characters.Length) ? characters[i] : null;
//     public ActorDef GetVehicleByIndex(int i)   => (i >= 0 && i < vehicles.Length) ? vehicles[i] : null;
// }

public class LoadoutCatalog : ScriptableObject
{
    public ActorDef[] characters;
    public ActorDef[] vehicles;

    public ActorDef GetCharacter(string id) => characters.FirstOrDefault(x => x && x.id == id);
    public ActorDef GetVehicle(string id)   => vehicles.FirstOrDefault(x => x && x.id == id);

    public ActorDef GetCharacterByIndex(int i) => (i >= 0 && i < characters.Length) ? characters[i] : null;
    public ActorDef GetVehicleByIndex(int i)   => (i >= 0 && i < vehicles.Length) ? vehicles[i] : null;
}
