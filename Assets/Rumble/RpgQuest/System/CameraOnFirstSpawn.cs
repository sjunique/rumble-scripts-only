using UnityEngine;

using System.Collections;
 

public class CameraOnFirstSpawn : MonoBehaviour
{
    bool _done;

    void OnEnable()  { PlayerCarSpawner.OnPlayerSpawned += Handle; }
    void OnDisable() { PlayerCarSpawner.OnPlayerSpawned -= Handle; }

    void Handle(GameObject player)
    {
        if (_done || !player) return;
        _done = true;
        StartCoroutine(DelayAndRestore(player));
    }

    IEnumerator DelayAndRestore(GameObject player)
    {
        yield return null; // let scene settle
        SceneCameraUtil.RestoreGameplayCamera(player);
    }
}
