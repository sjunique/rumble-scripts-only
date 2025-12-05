using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UISelectUtil
{
    public static void Focus(Button b)
    {
        if (!b) return;
        var es = EventSystem.current;
        if (!es) return;
        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(b.gameObject);
        b.OnSelect(null);
    }
}
