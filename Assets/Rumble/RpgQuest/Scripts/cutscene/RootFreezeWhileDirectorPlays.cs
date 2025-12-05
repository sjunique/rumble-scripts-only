using UnityEngine;
using UnityEngine;
using UnityEngine.Playables;

public class RootFreezeWhileDirectorPlays : MonoBehaviour
{
    [SerializeField] PlayableDirector director;
    [SerializeField] Transform standPad;

    Vector3 _savedPos;
    Quaternion _savedRot;
    bool _active;

    void Awake()
    {
        if (director) director.stopped += OnStopped;
    }
    void OnDestroy()
    {
        if (director) director.stopped -= OnStopped;
    }

    public void Arm()  // call right before director.Play()
    {
        if (!director || !standPad) return;
        _savedPos = transform.position;
        _savedRot = transform.rotation;
        _active = true;
        SnapToPad();
    }

    void LateUpdate()
    {
        if (_active && director && director.state == PlayState.Playing)
            SnapToPad(); // pin every frame so no drifting/“mushing”
    }

    void OnStopped(PlayableDirector d)
    {
        _active = false;
        // stay where the cutscene ended; if you prefer restoring:
        // transform.SetPositionAndRotation(_savedPos, _savedRot);
    }

    void SnapToPad()
    {
        transform.SetPositionAndRotation(standPad.position, standPad.rotation);
    }
}
