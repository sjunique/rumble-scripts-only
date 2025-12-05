using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] float hoverScale = 1.03f;
    [SerializeField] float pressScale = 0.98f;
    [SerializeField] float lerpSpeed = 14f;
    [SerializeField] AudioSource uiAudio;     // optional
    [SerializeField] AudioClip hoverClip, clickClip;

    Vector3 _base = Vector3.one, _target = Vector3.one;
    bool _pressed;

    void Awake() { _base = transform.localScale; _target = _base; }
    void Update() { transform.localScale = Vector3.Lerp(transform.localScale, _target, Time.unscaledDeltaTime * lerpSpeed); }

    public void OnPointerEnter(PointerEventData e) { SetHover(true); Play(hoverClip); }
    public void OnPointerExit(PointerEventData e) { SetHover(false); }
    public void OnPointerDown(PointerEventData e) { _pressed = true; _target = _base * pressScale; }
    public void OnPointerUp(PointerEventData e) { _pressed = false; _target = _base * hoverScale; Play(clickClip); }
    public void OnSelect(BaseEventData e) { SetHover(true); }
    public void OnDeselect(BaseEventData e) { SetHover(false); }

    void SetHover(bool on) { if (!_pressed) _target = _base * (on ? hoverScale : 1f); }
    void Play(AudioClip c) { if (uiAudio && c) uiAudio.PlayOneShot(c, 0.7f); }
}
