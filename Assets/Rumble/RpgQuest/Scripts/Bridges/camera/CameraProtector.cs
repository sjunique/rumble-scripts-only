using UnityEngine;

 

public class CameraProtector : MonoBehaviour
{
    public Camera backupCameraPrefab; // small fallback camera prefab (assign in inspector)
    private GameObject _activeCam;

    void Awake()
    {
        _activeCam = Camera.main ? Camera.main.gameObject : null;
    }

    void Update()
    {
        if (_activeCam == null || _activeCam.Equals(null))
        {
            Debug.LogWarning("[CameraProtector] Active main camera is null; spawning backup camera.");
            if (backupCameraPrefab != null)
            {
                var go = Instantiate(backupCameraPrefab.gameObject);
                go.name = "BackupMainCamera";
                go.tag = "MainCamera";
                DontDestroyOnLoad(go);
                _activeCam = go;
                Debug.Log("[CameraProtector] Backup camera spawned and preserved.");
            }
        }
    }
}
