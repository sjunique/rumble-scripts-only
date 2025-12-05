using UnityEngine;

public class TriggerProxyRelay : MonoBehaviour
{
    void OnTriggerEnter(Collider other) =>
        SendToParent(other, "OnTriggerEnter");
    void OnTriggerExit(Collider other) =>
        SendToParent(other, "OnTriggerExit");

    void SendToParent(Collider other, string fn)
    {
        var parent = transform.root;
        if (parent)
            parent.SendMessage(fn, other, SendMessageOptions.DontRequireReceiver);
    }
}
