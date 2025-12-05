using UnityEngine;
using Invector.vCharacterController;

public class PlayerInputDeepProbe : MonoBehaviour
{
    public vThirdPersonInput inv;

    public KeyCode dumpKey = KeyCode.F1;

    void Reset() { if (!inv) inv = GetComponent<vThirdPersonInput>(); }

    void Update()
    {
        if (Input.GetKeyDown(dumpKey)) Dump();
    }

    void Dump()
    {
        // A) Invector wrappers
        float wX = (inv && inv.horizontalInput != null) ? inv.horizontalInput.GetAxisRaw() : float.NaN;
        float wZ = (inv && inv.verticalInput   != null) ? inv.verticalInput.GetAxisRaw()   : float.NaN;

        // B) Old Input Manager axes
        float axX = 0f, axZ = 0f;
        try { axX = Input.GetAxisRaw("Horizontal"); axZ = Input.GetAxisRaw("Vertical"); } catch {}

        // C) Direct key fallback
        int kx = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
               - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)  ? 1 : 0);
        int kz = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)    ? 1 : 0)
               - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)  ? 1 : 0);

        Debug.Log($"[DEEP] wrapper=({wX},{wZ}) axes=({axX},{axZ}) keys=({kx},{kz})");
    }
}
