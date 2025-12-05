 

using UnityEngine;
using Invector;


 
using UnityEngine.AI;
 
[DisallowMultipleComponent]
public class DeathVelocityGuard : MonoBehaviour
{
    Rigidbody _rb;
    vHealthController _hc;
    NavMeshAgent _agent;

    void Awake()
    {
        _rb    = GetComponent<Rigidbody>();
        _hc    = GetComponent<vHealthController>();
        _agent = GetComponent<NavMeshAgent>();

      
   if (_hc != null)
             _hc.onDead.AddListener(go => OnDeadZeroOnce());


    }

    void OnDestroy()
    {
             
   if (_hc != null)
             _hc.onDead.AddListener(go => OnDeadZeroOnce());
    }

    // Called exactly once when death is signaled
    void OnDeadZeroOnce()
    {
        // Stop any agent motion first
        if (_agent)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // Only touch velocities while NON-kinematic to avoid Unity’s warning
        if (_rb && !_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // After this frame, other systems may flip kinematic; we won’t write to velocity anymore.
        // If something keeps forcing motion, consider disabling that component here.
        // e.g., GetComponent<YourMotor>()?.enabled = false;
    }

    // Safety net: if something re-enables motion while still alive & NON-kinematic, damp it.
    void LateUpdate()
    {
        if (_rb == null || _hc == null) return;

        // Only clamp when alive & non-kinematic to stay warning-free
        if (!_hc.isDead && !_rb.isKinematic)
        {
            // tiny damping to prevent residual slides
            if (_rb.linearVelocity.sqrMagnitude > 0.0004f) _rb.linearVelocity *= 0.9f;
            if (_rb.angularVelocity.sqrMagnitude > 0.0004f) _rb.angularVelocity *= 0.9f;
        }
    }
}




// [DisallowMultipleComponent]
// public class DeathVelocityGuard : MonoBehaviour
// {
//     Rigidbody _rb;
//     vHealthController _hc;

//     void Awake()
//     {
//         _rb = GetComponent<Rigidbody>();
//         _hc = GetComponent<vHealthController>();

//         // If the health exposes a death event, do a one-shot zero on that exact frame
//         if (_hc != null)
//             _hc.onDead.AddListener(go => OnDeadZero());
//     }

//     void OnDestroy()
//     {
//         if (_hc != null)
//             _hc.onDead.RemoveListener(go => OnDeadZero());
//     }

//     void OnDeadZero()
//     {
//         if (_rb == null) return;
//         _rb.linearVelocity = Vector3.zero;
//         _rb.angularVelocity = Vector3.zero;
//     }

//     // Safety net: if some system toggles kinematic mid-frame, kill velocities so no writer hits a kinematic RB.
//     void LateUpdate()
//     {
//         if (_rb == null) return;
//         if (_rb.isKinematic)
//         {
//             _rb.linearVelocity = Vector3.zero;
//             _rb.angularVelocity = Vector3.zero;
//         }
//     }
// }
