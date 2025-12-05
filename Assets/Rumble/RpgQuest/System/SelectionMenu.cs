using UnityEngine;

public class SelectionMenu : MonoBehaviour
{
    public GameFlow flow;                 // assign (in Bootstrap or in this scene)
    public string levelKeyToLoad = "Level_0_Main"; // or Level_1_Enchanted

    public void SelectPlayerByIndex(int index)
    {
        SelectionRuntime.Instance.ChoosePlayer(index);
        Debug.Log($"[Selection] Player index set to {index}");
    }

    public void SelectVehicleByIndex(int index)
    {
        SelectionRuntime.Instance.ChooseVehicle(index);
        Debug.Log($"[Selection] Vehicle index set to {index}");
    }

    public void Play()
    {
        // calls the async loader in GameFlow (additive load of your level)
        flow.PlayLevel(levelKeyToLoad);
    }
}
