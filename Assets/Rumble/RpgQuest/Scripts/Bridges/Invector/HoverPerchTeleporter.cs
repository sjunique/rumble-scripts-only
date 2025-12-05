using UnityEngine;
using System.Collections;
using Invector.vCharacterController;

 
 
using Unity.Cinemachine;
using Invector.vCharacterController;

public class HoverPerchTeleporter : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private GameObject carObject;
    [SerializeField] private vThirdPersonController playerController;
    [SerializeField] private CinemachineCamera carTopDownCam;
    [SerializeField] private CinemachineVirtualCameraBase playerFollowCam; // Your normal player cam

    [Header("SETTINGS")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Y;
    [SerializeField] private float playerHeightOffset = 2f;

    private GameObject playerVisuals;
    private bool isInCar = false;
    private CinemachineBrain brain;

    void Awake()
    {
        // Find required components
        playerVisuals = playerController.transform.Find("VisualModel")?.gameObject 
                      ?? playerController.gameObject;
        
        brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain == null)
            Debug.LogError("No CinemachineBrain found on main camera!");

        // Initialize state
        if (carObject != null) carObject.SetActive(false);
        if (carTopDownCam != null) carTopDownCam.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"[HoverPerchTeleporter] Toggle pressed. Current state: ");
            if (isInCar) ExitCar();
            else EnterCar();
        }
    }

    void EnterCar()
    {
        // Disable player
        playerController.enabled = false;
        playerVisuals.SetActive(false);
        
        // Position player (hidden inside car)
        playerController.transform.position = carObject.transform.position 
                                           + Vector3.up * playerHeightOffset;
        
        // Activate car system
        carObject.SetActive(true);
        
        // Switch to car camera
        if (carTopDownCam != null)
        {
            carTopDownCam.enabled = true;
            carTopDownCam.Priority = 100;
            
            // Disable player camera
            if (playerFollowCam != null)
                playerFollowCam.Priority = 0;
        }

        isInCar = true;
        DebugCameraState();
    }

    void ExitCar()
    {
        // Reactivate player
        playerController.enabled = true;
        playerVisuals.SetActive(true);
        
        // Position player behind car
        playerController.transform.position = carObject.transform.position 
                                           + carObject.transform.forward * 3f
                                           + Vector3.up * 0.5f;
        
        // Deactivate car
        carObject.SetActive(false);
        
        // Switch back to player camera
        if (playerFollowCam != null)
        {
            playerFollowCam.Priority = 100;
            if (carTopDownCam != null)
                carTopDownCam.Priority = 0;
        }

        isInCar = false;
        DebugCameraState();
    }

    void DebugCameraState()
    {
        string camName = brain.ActiveVirtualCamera?.Name ?? "None";
        Debug.Log($"Car mode: {isInCar} | Active camera: {camName}");
    }
}

/*
public class HoverPerchTeleporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private vThirdPersonController _player;
    [SerializeField] private Transform _perch;
    [SerializeField] private ActualHoverController _hoverController;
    [SerializeField] private KeyCode _toggleKey = KeyCode.T;
    [SerializeField] private Collider _perchCollider;

    [Header("Settings")]
    [SerializeField] private float _hoverHeight = 3f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private LayerMask _groundLayer;

    private bool _isPlayerMounted;
    private bool _isMoving;
    private Rigidbody _perchRigidbody;

    void Start()
    {
        _perchRigidbody = _perch.GetComponent<Rigidbody>();
        _perchCollider.enabled = false;
        _hoverController.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            if (_isPlayerMounted)
                StartCoroutine(ReturnToGround());
            else if (!_isMoving)
                StartCoroutine(MovePerchToPlayer());
        }
    }

    private IEnumerator MovePerchToPlayer()
    {
        _isMoving = true;
        _perchCollider.enabled = false;

        // Disable hover controller (gravity will be handled manually)
        _hoverController.enabled = false;
        _perchRigidbody.useGravity = false; // Prevent gravity during ascent

        // Move perch above player
        Vector3 targetPos = _player.transform.position + Vector3.up * _hoverHeight;
        while (Vector3.Distance(_perch.position, targetPos) > 0.1f)
        {
            _perchRigidbody.MovePosition(
                Vector3.Lerp(_perch.position, targetPos, _moveSpeed * Time.deltaTime)
            );
            yield return null;
        }

        MountPlayer();
        _perchCollider.enabled = true;
        _isMoving = false;
    }

    private IEnumerator ReturnToGround()
    {
        _isMoving = true;
        _perchCollider.enabled = false;

        // Force-enable gravity (override hover controller)
        _hoverController.enabled = false;
        _perchRigidbody.useGravity = true;
        _perchRigidbody.linearVelocity = Vector3.zero; // Reset velocity

        // Wait for perch to land
        Vector3 groundPos = FindGroundPosition(_player.transform.position);
        while (_perch.position.y > groundPos.y + 0.1f)
        {
            yield return null;
        }

        // Snap to ground and disable gravity
        _perch.position = groundPos + Vector3.up * 0.1f;
        _perchRigidbody.useGravity = false;

        DismountPlayer();
        _perchCollider.enabled = true;
        _isMoving = false;
    }

    private void MountPlayer()
    {
        _player.transform.SetParent(_perch);
        _player.transform.localPosition = Vector3.zero;
        _player.isImmune = true;
        _hoverController.enabled = true; // Re-enable hover controller
        _isPlayerMounted = true;
    }

    private void DismountPlayer()
    {
        _player.transform.SetParent(null);
        _player.isImmune = false;
        _hoverController.enabled = false;
        _isPlayerMounted = false;
    }

    private Vector3 FindGroundPosition(Vector3 startPos)
    {
        RaycastHit hit;
        if (Physics.Raycast(startPos + Vector3.up * 10f, Vector3.down, out hit, Mathf.Infinity, _groundLayer))
            return hit.point;
        return startPos;
    }
}
*/
