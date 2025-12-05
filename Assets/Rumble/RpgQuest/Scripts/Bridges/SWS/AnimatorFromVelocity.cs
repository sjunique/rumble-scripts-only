using UnityEngine;
public class AnimatorFromVelocity : MonoBehaviour
{
    public Animator anim;
    public string speedParam = "Speed";   // change to your param name
    public string movingBool = "IsMoving";
    Vector3 lastPos;
    void Awake(){ if(!anim) anim = GetComponentInChildren<Animator>(); lastPos = transform.position; }
    void Update(){
        var v = (transform.position - lastPos) / Mathf.Max(Time.deltaTime, 1e-4f);
        float s = v.magnitude;
        if(anim){
            anim.SetFloat(speedParam, s);
            if(!string.IsNullOrEmpty(movingBool)) anim.SetBool(movingBool, s > 0.1f);
        }
        lastPos = transform.position;
    }
}
