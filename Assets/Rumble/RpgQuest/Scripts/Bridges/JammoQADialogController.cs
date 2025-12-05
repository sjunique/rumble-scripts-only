using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class JammoQADialogController : MonoBehaviour
{
    UIDocument doc; VisualElement root;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;
        Hide();
    }

    public void Show(string title=null, string body=null, string legend=null)
    {
      //  if (!string.IsNullOrEmpty(title))  root.Q<Label>("title")?.SetValueWithoutNotify(title);
      //  if (!string.IsNullOrEmpty(body))   root.Q<Label>("body")?.SetValueWithoutNotify(body);
      //  if (!string.IsNullOrEmpty(legend)) root.Q<Label>("legend")?.SetValueWithoutNotify(legend);
        root.style.display = DisplayStyle.Flex;
    }

    public void Hide() => doc.rootVisualElement.style.display = DisplayStyle.None;
}

