using UnityEngine;

public class ImmunityFlag : MonoBehaviour
{
    public bool IsImmune { get; private set; }

    public void SetImmune(bool value) => IsImmune = value;

    public void SetImmuneForSeconds(MonoBehaviour host, float seconds)
    {
        host.StartCoroutine(CoImmune(seconds));
    }

    private System.Collections.IEnumerator CoImmune(float s)
    {
        IsImmune = true;
        yield return new WaitForSeconds(s);
        IsImmune = false;
    }
}