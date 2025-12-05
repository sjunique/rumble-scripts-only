using UnityEngine;

public class PreviewModelRotator : MonoBehaviour
{
    public float rotationSpeed = 15f;
    public bool rotateRight = true;
    
    void Update()
    {
        float direction = rotateRight ? 1f : -1f;
        transform.Rotate(0, rotationSpeed * direction * Time.deltaTime, 0);
    }
    
    // Call this to change rotation direction
    public void ToggleRotation()
    {
        rotateRight = !rotateRight;
    }
}
