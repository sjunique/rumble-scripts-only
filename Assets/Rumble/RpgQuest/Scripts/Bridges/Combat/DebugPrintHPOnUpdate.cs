using UnityEngine;
using Invector.vCharacterController;
using Invector;
[RequireComponent(typeof(vHealthController))]
public class DebugPrintHPOnUpdate : MonoBehaviour
{
    vHealthController h; float last;
    void Awake(){ h = GetComponent<vHealthController>(); last = h.currentHealth; }
    void LateUpdate(){
        if (!h) return;
        if (Mathf.Abs(h.currentHealth - last) > 0.01f) {
            Debug.Log($"HP: {h.currentHealth}/{h.MaxHealth}");
            last = h.currentHealth;
        }
    }
}
