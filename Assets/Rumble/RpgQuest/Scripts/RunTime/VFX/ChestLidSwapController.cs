using System.Collections;
using UnityEngine;

public class ChestLidSwapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform lidPivot;
    [SerializeField] private GameObject closedLid;
    [SerializeField] private GameObject openedLid;
    [SerializeField] private GameObject gold;
    [SerializeField] private BeaconVFXActivator beacon;

    [Header("Animation")]
    [SerializeField] private float openAngle = -90f;   // try positive if opens opposite way
    [SerializeField] private Vector3 axis = Vector3.right;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private AnimationCurve ease = null;

    Quaternion _closedRot, _openRot;
    bool _isOpen;
    Coroutine _co;

    void Awake()
    {
        if (ease == null) ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
        _closedRot = lidPivot.localRotation;
        _openRot = _closedRot * Quaternion.AngleAxis(openAngle, axis);

        // ensure start state
        closedLid.SetActive(true);
        openedLid.SetActive(false);
        gold.SetActive(false);
    }

    void OnEnable()
    {
        if (beacon != null)
        {
            beacon.onActivated.AddListener(Open);
            beacon.onDeactivated.AddListener(Close);
        }
    }

    void OnDisable()
    {
        if (beacon != null)
        {
            beacon.onActivated.RemoveListener(Open);
            beacon.onDeactivated.RemoveListener(Close);
        }
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Co_Open());
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Co_Close());
    }

    IEnumerator Co_Open()
    {
        Quaternion from = _closedRot;
        Quaternion to = _openRot;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            lidPivot.localRotation = Quaternion.Slerp(from, to, ease.Evaluate(t));
            yield return null;
        }

        closedLid.SetActive(false);
        openedLid.SetActive(true);
        gold.SetActive(true);
    }

    IEnumerator Co_Close()
    {
        closedLid.SetActive(true);
        openedLid.SetActive(false);
        gold.SetActive(false);

        Quaternion from = _openRot;
        Quaternion to = _closedRot;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            lidPivot.localRotation = Quaternion.Slerp(from, to, ease.Evaluate(t));
            yield return null;
        }
    }
}
