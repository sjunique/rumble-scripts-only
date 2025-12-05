using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PausePanelController : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private bool pauseOnOpen = true;
    [SerializeField] private bool lockCursor = false;

    CanvasGroup cg;
    bool isOpen;

    void Awake() { cg = GetComponent<CanvasGroup>(); HideImmediate(); }

    public void Open()
    {
        isOpen = true;
        cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
        if (pauseOnOpen) Time.timeScale = 0f;
        if (lockCursor) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    public void Close()
    {
        isOpen = false;
        HideImmediate();
        if (pauseOnOpen) Time.timeScale = 1f;
    }

    void HideImmediate()
    {
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
    }

    public void Toggle()
    {
        if (isOpen) Close(); else Open();
    }
}
