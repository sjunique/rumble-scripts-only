using UnityEngine;

 

public class UIOverlayManager : MonoBehaviour
{
    [SerializeField] private PausePanelController shopPanel;
    [SerializeField] private KeyCode openShopKey = KeyCode.Tab; // or I

    void Update()
    {
        if (Input.GetKeyDown(openShopKey))
            shopPanel?.Toggle();
    }
}
