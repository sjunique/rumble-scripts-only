using UnityEngine;

public class EnableOnClick : MonoBehaviour
{
    public GameObject target;
    public void Go()
    {
        if (!target) { Debug.LogError("[EnableOnClick] Target is NULL"); return; }
        Debug.Log("[EnableOnClick] Enabling " + target.transform.GetHierarchyPath());
        target.SetActive(true);
    }
}
