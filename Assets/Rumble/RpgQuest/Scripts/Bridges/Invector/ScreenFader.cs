using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class ScreenFader : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas rootCanvas;        // optional; auto-filled
    [SerializeField] private CanvasGroup canvasGroup;  // required: the black panel group

    [Header("Defaults")]
    [SerializeField] private float defaultFadeDuration = 0.25f;
    [SerializeField] private bool startHidden = true;  // alpha=0 on Start

    void Reset()
    {
        rootCanvas  = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup)
        {
            Debug.LogError("[ScreenFader] Missing CanvasGroup. Add this component to your full-screen panel.");
            enabled = false; return;
        }

        if (startHidden) canvasGroup.alpha = 0f;
        // Make sure this blocks input while visible (so clicks don’t leak through during fade out)
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable   = false;
    }

    public IEnumerator FadeOut(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultFadeDuration;
        yield return FadeTo(1f, duration);
    }

    public IEnumerator FadeIn(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultFadeDuration;
        yield return FadeTo(0f, duration);
    }

    public IEnumerator FadeOutAndIn(System.Action midAction, float outDur = -1f, float inDur = -1f, float hold = 0f)
    {
        yield return FadeOut(outDur);
        if (hold > 0f) yield return new WaitForSeconds(hold);
        midAction?.Invoke();
        yield return FadeIn(inDur);
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start  = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // unscaled so timescale changes don’t stall fade
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        canvasGroup.alpha = target;
    }
}
