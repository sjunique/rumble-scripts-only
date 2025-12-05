

using UnityEngine;
public class DamageProbe : MonoBehaviour
{
    public void DamageInvectorPlayer(int d, Transform a) { Debug.Log($"Probe: DamageInvectorPlayer {d} from {a?.name}"); }
    public void DamageCharacterController(int d, Transform a) { Debug.Log($"Probe: DamageCharacterController {d} from {a?.name}"); }
    public void DamagePlayer(int d, Transform a) { Debug.Log($"Probe: DamagePlayer {d} from {a?.name}"); }
}
