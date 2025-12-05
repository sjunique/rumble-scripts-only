using UnityEngine;
public class PauseHotkey : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var gf = FindObjectOfType<GameFlow>(true);
          //  if (gf != null) gf.TogglePause();
        }
    }
}
