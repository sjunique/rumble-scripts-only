using UnityEngine;
using System.Collections;

public class CameraFollowBinder : MonoBehaviour
{
    void OnEnable()
    {
        PlayerCarSpawner.OnPlayerSpawned += OnPlayerSpawned;
    }

    void OnDisable()
    {
        PlayerCarSpawner.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(GameObject player)
    {
        StartCoroutine(BindNextFrame(player));
    }

    private IEnumerator BindNextFrame(GameObject player)
    {
        if (!player) yield break;
        yield return null;

        var camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (!camObj)
        {
            Debug.LogError("[CameraFollowBinder] No MainCamera found.");
            yield break;
        }

        var invectorCam =
            camObj.GetComponent<Invector.vCamera.vThirdPersonCamera>() ??
            camObj.GetComponent<Invector.vCamera.vThirdPersonCamera>();

        if (!invectorCam)
        {
            Debug.LogError("[CameraFollowBinder] MainCamera found but no Invector camera component.");
            yield break;
        }

        invectorCam.mainTarget = player.transform;

        Debug.Log("[CameraFollowBinder] MainCamera → " + player.name);
    }
}
