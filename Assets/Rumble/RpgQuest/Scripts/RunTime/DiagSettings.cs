using UnityEngine;

 
 

[CreateAssetMenu(menuName = "Diagnostics/Diag Settings", fileName = "DiagSettings")]
public class DiagSettings : ScriptableObject
{
    public bool      enabled = true;
    public DiagLevel level   = DiagLevel.Info;
}

 
