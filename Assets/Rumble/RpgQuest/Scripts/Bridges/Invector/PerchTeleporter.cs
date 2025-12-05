using UnityEngine;

 

public class PerchTeleporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform perch;
    [SerializeField] private KeyCode teleportKey = KeyCode.P;
    [SerializeField] private MonoBehaviour hudScript; // Drag your HUD script here
    
    [Header("Camera Settings")]
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject vantageCamera;
    
    [Header("Offset Settings")]
    [SerializeField] private Vector3 perchPositionOffset = new Vector3(0f, 3f, 2f);
    [SerializeField] private float playerStandingOffset = 0.2f;

    private BoxCollider _perchCollider;
    private bool _isOnPerch = false;

    void Start()
    {
        _perchCollider = perch.GetComponent<BoxCollider>();
        if (!_perchCollider) Debug.LogError("Missing BoxCollider on perch!");
    }

    void Update()
    {
        if (Input.GetKeyDown(teleportKey))
        {
            if (_isOnPerch)
            {
                ReturnFromPerch();
            }
            else
            {
                TeleportToPerch();
            }
        }
    }

    private void TeleportToPerch()
    {
        // Position perch
        Vector3 perchWorldPosition = player.position + 
                                   (player.forward * perchPositionOffset.z) + 
                                   (player.up * perchPositionOffset.y);
        perch.position = perchWorldPosition;

        // Teleport player
        Vector3 perchSurface = perch.position + 
                             new Vector3(0f, _perchCollider.size.y * 0.5f, 0f);
        player.position = perchSurface + (Vector3.up * playerStandingOffset);

        // Switch cameras and disable HUD
        mainCamera.SetActive(false);
        vantageCamera.SetActive(true);
        if (hudScript != null) hudScript.enabled = false;
        
        _isOnPerch = true;
    }

    private void ReturnFromPerch()
    {
        // Return to normal view
        mainCamera.SetActive(true);
        vantageCamera.SetActive(false);
        if (hudScript != null) hudScript.enabled = true;
        
        _isOnPerch = false;
    }
}
/*
public class PerchTeleporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform perch; // Your empty GameObject with BoxCollider
    [SerializeField] private KeyCode teleportKey = KeyCode.T;

    [Header("Offset Settings")]
    [SerializeField] private Vector3 perchPositionOffset = new Vector3(0f, 3f, 2f); // Customizable offset
    [SerializeField] private float playerStandingOffset = 0.2f; // Prevents clipping

    private BoxCollider _perchCollider;

    void Start()
    {
        _perchCollider = perch.GetComponent<BoxCollider>();
        if (_perchCollider == null)
            Debug.LogError("Perch needs a BoxCollider!");
    }

    void Update()
    {
        if (Input.GetKeyDown(teleportKey))
        {
            TeleportPlayerToPerch();
        }
    }

    private void TeleportPlayerToPerch()
    {
        // Calculate perch position relative to player
        Vector3 perchWorldPosition = player.position + 
                                   (player.forward * perchPositionOffset.z) + 
                                   (player.up * perchPositionOffset.y) + 
                                   (player.right * perchPositionOffset.x);

        // Snap perch to this position (if using dynamic perch)
        perch.position = perchWorldPosition;

        // Teleport player on top of perch
        Vector3 perchSurface = perch.position + 
                             new Vector3(0f, _perchCollider.size.y * 0.5f, 0f);

        player.position = perchSurface + (Vector3.up * playerStandingOffset);
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (player == null || perch == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(perch.position, _perchCollider.size);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(player.position, 0.2f);
    }
}

*/
