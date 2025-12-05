using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PointsCounterUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private Image currencyIcon;      // optional (e.g., coin icon)

    [Header("Formatting")]
    [SerializeField] private string prefix = "Points: ";
    [SerializeField] private bool useThousandsSeparator = true; // 1,234

    [Header("Feedback")]
    [SerializeField] private bool pulseOnChange = true;
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseIn = 0.07f;
    [SerializeField] private float pulseHold = 0.05f;
    [SerializeField] private float pulseOut = 0.07f;

    private Coroutine _pulseCo;

    void OnEnable()
    {
        RefreshNow(); // initial draw

        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnPointsChanged += HandlePointsChanged;
            UpgradeStateManager.Instance.OnStateLoaded += RefreshNow;
        }
    }

    void OnDisable()
    {
        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnPointsChanged -= HandlePointsChanged;
            UpgradeStateManager.Instance.OnStateLoaded -= RefreshNow;
        }
    }

    private void HandlePointsChanged(int oldPts, int newPts)
    {
        SetText(newPts);
        if (pulseOnChange && newPts != oldPts) Pulse();
    }

    private void RefreshNow()
    {
        if (UpgradeStateManager.Instance == null) return;
        SetText(UpgradeStateManager.Instance.Points);
    }

    private void SetText(int value)
    {
        if (!pointsText) return;

        // Format
        string num = useThousandsSeparator ? value.ToString("N0") : value.ToString();
        pointsText.text = $"{prefix}{num}";
    }

    private void Pulse()
    {
        if (!pointsText) return;

        if (_pulseCo != null) StopCoroutine(_pulseCo);
        _pulseCo = StartCoroutine(CoPulse(pointsText.rectTransform));
    }

    private IEnumerator CoPulse(RectTransform rt)
    {
        Vector3 start = Vector3.one;
        Vector3 peak  = Vector3.one * pulseScale;

        float t = 0f;
        while (t < pulseIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / pulseIn);
            rt.localScale = Vector3.Lerp(start, peak, k);
            yield return null;
        }
        rt.localScale = peak;

        yield return new WaitForSecondsRealtime(pulseHold);

        t = 0f;
        while (t < pulseOut)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / pulseOut);
            rt.localScale = Vector3.Lerp(peak, start, k);
            yield return null;
        }
        rt.localScale = start;

        _pulseCo = null;
    }
}
