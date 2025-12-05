using UnityEngine;
// Put this on the test trigger
using UnityEngine;
public class WaterTriggerDebug : MonoBehaviour {
    void OnTriggerEnter(Collider c){ if (c.CompareTag("Player")) Debug.Log("ENTER water"); }
    void OnTriggerStay(Collider c){ if (c.CompareTag("Player")) Debug.Log("STAY water"); }
    void OnTriggerExit (Collider c){ if (c.CompareTag("Player")) Debug.Log("EXIT water"); }
}
