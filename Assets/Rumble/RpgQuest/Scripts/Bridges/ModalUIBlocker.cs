using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModalUIBlocker : MonoBehaviour
{
    [Tooltip("uGUI roots that should NOT intercept clicks while a modal is open")]
    [SerializeField] private List<GameObject> uGuiRoots = new List<GameObject>(); // HUD, AimCanvas, StunEffectCanvas

    [Tooltip("Also unlock and show the cursor while modal is open")]
    [SerializeField] private bool unlockCursor = true;

    private struct RaycasterState { public GraphicRaycaster gr; public bool enabled; }
    private readonly List<RaycasterState> saved = new();

    public void Begin()
    {
        saved.Clear();

        foreach (var root in uGuiRoots)
        {
            if (!root) continue;
            foreach (var gr in root.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                saved.Add(new RaycasterState { gr = gr, enabled = gr.enabled });
                gr.enabled = false;
            }
        }

        if (unlockCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void End()
    {
        foreach (var s in saved)
            if (s.gr) s.gr.enabled = s.enabled;
        saved.Clear();
    }
}
