using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class DisableDecorRaycasts : MonoBehaviour
{
    [ContextMenu("Disable Raycast Target on Decorative Graphics")]
    void DisableAll()
    {
        var graphics = GetComponentsInChildren<Graphic>(true);
        int changed = 0;
        foreach (var g in graphics)
        {
            // Keep raycasts ON for known interactive roots (Button/Toggle/Slider)
            bool isInteractive = g.GetComponent<Button>() || g.GetComponent<Toggle>() || g.GetComponent<Slider>();
            if (!isInteractive && g.raycastTarget)
            {
                g.raycastTarget = false;
                changed++;
            }
        }
        Debug.Log($"[DisableDecorRaycasts] Turned off raycastTarget on {changed} decorative graphics.");
    }
}
