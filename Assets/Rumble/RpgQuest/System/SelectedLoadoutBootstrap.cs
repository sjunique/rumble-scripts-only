// SelectedLoadoutBootstrap.cs
using UnityEngine;

public class SelectedLoadoutBootstrap : MonoBehaviour
{
    [SerializeField] SelectedLoadout loadoutPrefab; // assign the prefab with catalog wired

    void Awake()
    {
        if (SelectedLoadout.Instance == null)
            Instantiate(loadoutPrefab);

        if (SaveSystem.TryLoad(out var data))
        {
            if (!string.IsNullOrEmpty(data.selectedCharacterId))
                SelectedLoadout.Instance.SetCharacter(data.selectedCharacterId);
            if (!string.IsNullOrEmpty(data.selectedCarId))
                SelectedLoadout.Instance.SetVehicle(data.selectedCarId);
        }
    }
}

