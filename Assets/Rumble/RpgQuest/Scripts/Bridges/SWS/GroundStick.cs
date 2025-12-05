using UnityEngine;
public class GroundStick : MonoBehaviour
{
    public LayerMask groundMask = ~0;
    public float rayUp = 1.5f, rayDown = 3f, heightOffset = 0.05f;
    void LateUpdate(){
        var from = transform.position + Vector3.up * rayUp;
        if (Physics.Raycast(from, Vector3.down, out var hit, rayUp + rayDown, groundMask))
            transform.position = new Vector3(transform.position.x, hit.point.y + heightOffset, transform.position.z);
    }
}
