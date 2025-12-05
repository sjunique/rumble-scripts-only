using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverCarController : MonoBehaviour
{
    [Header("Hover Settings")]
    public Transform[] hoverPoints;
    public float hoverHeight = 2f;
    public float springStrength = 100f;
    public float damper = 5f;
  [Header("Controls")]
    public KeyCode parkKey = KeyCode.P;
    [Header("Drive Settings")]
    public float acceleration = 800f;
    public float turnStrength = 150f;

    [Header("Visual Banking")]
    public Transform visualBody;      // assign your car’s mesh root here
    public float maxBankAngle = 15f;
    public float bankSpeed = 3f;

    Rigidbody rb;
 private bool isParked = false;

    // Add this public method:
    public void ParkCar(float landTime = 1f)
    {
        if (isParked) return;
        isParked = true;
        StartCoroutine(ParkRoutine(landTime));
    }
    // ← Declare it here
    private Quaternion bodyStartRot;
      void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.down * 0.5f;

        // ← Capture the “rest” rotation of your visual body
        if (visualBody != null)
            bodyStartRot = visualBody.localRotation;
    }

  IEnumerator ParkRoutine(float duration)
    {
        // 1) Stop reading inputs
        //    (Your ApplyDriveForces/ApplyBanking can early‑out if isParked == true)
        
        // 2) Gradually lower the hoverHeight to zero
        float startHeight = hoverHeight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hoverHeight = Mathf.Lerp(startHeight, 0f, elapsed / duration);
            yield return null;
        }
        hoverHeight = 0f;

   
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        
        enabled = false;
    }

    void FixedUpdate()
    {
        if (isParked) return;
        ApplyHoverForces();
        ApplyDriveForces();
    }



    void Update()
    {
        if (!isParked)
        {
            // Banking, etc.
            ApplyBanking();

            // Check for park input:
            if (Input.GetKeyDown(parkKey))
            {
                ParkCar(1.0f);   // lands over 1 second
            }
        }
    }

   

   
    void ApplyHoverForces()
    {
        foreach (Transform hp in hoverPoints)
        {
            Ray ray = new Ray(hp.position, -transform.up);
            if (Physics.Raycast(ray, out RaycastHit hit, hoverHeight * 2f))
            {
                
                float displacement = hoverHeight - hit.distance;
                float springForce = displacement * springStrength;

          
                float velocityAlongRay = Vector3.Dot(rb.GetPointVelocity(hp.position), -transform.up);
                float damperForce = velocityAlongRay * damper;

                Vector3 force = (springForce - damperForce) * transform.up;
                rb.AddForceAtPosition(force, hp.position);
            }
        }
    }

   

void ApplyDriveForces()
{
    float forward = Input.GetAxis("Vertical");
    float turn    = Input.GetAxis("Horizontal");

    // Forward/backward force
    Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    rb.AddForce(flatForward * forward * acceleration, ForceMode.Acceleration);

  //   rb.AddTorque(Vector3.up * turn * turnStrength, ForceMode.VelocityChange);
   rb.AddTorque(Vector3.up * turn * turnStrength * Time.fixedDeltaTime, ForceMode.Acceleration);
}



  void ApplyBanking()
{
    if (visualBody == null) return;

    float turn = Input.GetAxis("Horizontal");  
    float targetBank = -turn * maxBankAngle;
    Quaternion bankQuat = Quaternion.Euler(0f, 0f, targetBank);

    visualBody.localRotation = Quaternion.Slerp(
        visualBody.localRotation,
        bodyStartRot * bankQuat,
        Time.deltaTime * bankSpeed
    );
}
}
