
using UnityEngine;
public class ShopUpgradePanelController : MonoBehaviour
{
    [SerializeField] CanvasGroup root;            // or UIDocument root, etc.
    [SerializeField] PlayerUIModalBlocker blocker;

    public void Open()
    {
        blocker?.Begin();
        if (root) { root.alpha = 1; root.blocksRaycasts = true; root.interactable = true; }
        gameObject.SetActive(true);
    }

    public void Close()
    {
        blocker?.End();
        if (root) { root.alpha = 0; root.blocksRaycasts = false; root.interactable = false; }
        gameObject.SetActive(false);
    }

    void OnDisable() { blocker?.End(); } // safety, never leave HUD blocked
}
