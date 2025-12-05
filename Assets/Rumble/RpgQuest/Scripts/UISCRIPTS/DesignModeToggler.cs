using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public class DesignModeToggler : MonoBehaviour
{
    public UIDocument uiDocument;
    public string designClass = "design-mode";

    void OnEnable()
    {
        if (!uiDocument) uiDocument = GetComponent<UIDocument>();
        // Defer one tick to ensure tree exists
        uiDocument.rootVisualElement?.schedule.Execute(() =>
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                uiDocument.rootVisualElement.AddToClassList(designClass);
            else
                uiDocument.rootVisualElement.RemoveFromClassList(designClass);
#else
            uiDocument.rootVisualElement.RemoveFromClassList(designClass);
#endif
        }).ExecuteLater(0);
    }
}

