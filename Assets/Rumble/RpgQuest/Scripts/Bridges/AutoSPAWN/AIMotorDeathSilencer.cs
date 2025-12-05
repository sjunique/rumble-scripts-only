using UnityEngine;
using Invector;

[DisallowMultipleComponent]
public class AIMotorDeathSilencer : MonoBehaviour
{
    vHealthController _hc;
    MonoBehaviour _motor;    // vAIMotor (kept as MonoBehaviour to avoid hard dep)

    void Awake()
    {
        _hc    = GetComponent<vHealthController>();
        _motor = (MonoBehaviour)GetComponent("vAIMotor"); // finds vAIMotor by name

       

  if (_hc != null)
             _hc.onDead.AddListener(go => OnDead());

    }
    void OnDestroy()
    {

 if (_hc != null)
             _hc.onDead.AddListener(go => OnDead());

         
    }
    void OnDead()
    {
        if (_motor) _motor.enabled = false;  // stops UpdateLocomotion/StepOffset
    }
}

