using UnityEngine;

public class AutoBoxCollider : MonoBehaviour
{
    void Start()
    {
        AddAutoBoxCollider();
    }
    
    [ContextMenu("Add Auto Box Collider")]
    public void AddAutoBoxCollider()
    {
        // Get or add BoxCollider component
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider>();
        
        // Calculate bounds including all child renderers
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        // Set collider properties
        collider.center = transform.InverseTransformPoint(bounds.center);
        collider.size = bounds.size;
    }
}