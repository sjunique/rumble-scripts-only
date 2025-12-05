using UnityEngine;
// Assets/Scripts/System/SelectionState.cs
 

[CreateAssetMenu(menuName = "Rumble/Selection State")]
public class SelectionState : ScriptableObject
{
    [Header("Addressables addresses (keys)")]
    public string chosenPlayerKey = "Eve";
    public string chosenVehicleKey = "ScifiRover";
}
