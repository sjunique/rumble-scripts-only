// Assets/Scripts/Gliding/ParagliderRig.cs
using UnityEngine;

public class ParagliderRig : MonoBehaviour
{
    [Header("Required on Prefab")]
    public Rigidbody rb;      // assign the rigidbody on the prefab
    public Camera glideCam;   // assign the camera child on the prefab

    // Optional: hook an existing controller if you use the physics model I gave you
    public ParagliderController controller;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        if (!glideCam) glideCam = GetComponentInChildren<Camera>(true);
        if (!controller) controller = GetComponent<ParagliderController>();
    }
}
