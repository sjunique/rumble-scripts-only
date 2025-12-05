using UnityEngine;

public class VantagePointTrigger : MonoBehaviour
{
    public Transform vantagePoint; // Set in Inspector
    public Transform player;
    public GameObject mainCamera;
    public GameObject vantageCamera;
 [SerializeField] private KeyCode teleportKey = KeyCode.V;

    void Update()
    {
        if (Input.GetKeyDown(teleportKey))
        {
            TeleportToVantage();
          
        }
    }

    public void TeleportToVantage()



    {
        player.position = vantagePoint.position;
        mainCamera.SetActive(false);
        vantageCamera.SetActive(true);
    }
}

