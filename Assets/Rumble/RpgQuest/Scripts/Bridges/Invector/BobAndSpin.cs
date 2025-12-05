using UnityEngine;

public class BobAndSpin : MonoBehaviour
{
    public float bobAmp = 0.2f;    // up/down amount (m)
    public float bobSpeed = 1.6f;  // Hz-ish
    public float spinSpeed = 90f;  // deg/sec
    float baseY;

    void OnEnable(){ baseY = transform.position.y; }
    void Update(){
        var p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmp;
        transform.position = p;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }
}
