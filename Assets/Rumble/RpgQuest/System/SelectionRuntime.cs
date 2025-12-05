using UnityEngine;

public class SelectionRuntime : MonoBehaviour
{
    public static SelectionRuntime Instance { get; private set; }

    [Header("Chosen indices (0-based)")]
    public int playerIndex = 0;
    public int vehicleIndex = 0;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChoosePlayer(int index)  { playerIndex  = Mathf.Max(0, index); }
    public void ChooseVehicle(int index) { vehicleIndex = Mathf.Max(0, index); }
}
