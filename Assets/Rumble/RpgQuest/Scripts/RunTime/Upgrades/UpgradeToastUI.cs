using UnityEngine;
using TMPro;

public class UpgradeToastUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float hold = 1.2f;
    [SerializeField] private float fadeOut = 0.35f;

    private Coroutine _playing;

    void OnEnable()
    {
        if (toastGroup) { toastGroup.alpha = 0f; toastGroup.gameObject.SetActive(false); }

        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnUpgradeLevelChanged += HandleLevelChanged; // (UpgradeId,int,int)
            UpgradeStateManager.Instance.OnStateLoaded += HideInstant;
        }
    }

    void OnDisable()
    {
        if (UpgradeStateManager.Instance != null)
        {
            UpgradeStateManager.Instance.OnUpgradeLevelChanged -= HandleLevelChanged;
            UpgradeStateManager.Instance.OnStateLoaded -= HideInstant;
        }
    }

    private void HideInstant()
    {
        if (toastGroup)
        {
            if (_playing != null) StopCoroutine(_playing);
            toastGroup.alpha = 0f;
            toastGroup.gameObject.SetActive(false);
        }
    }

    private void HandleLevelChanged(UpgradeId id, int oldLevel, int newLevel)
    {
        // Ignore “no-op” changes
        if (newLevel == oldLevel) return;

        // Compose message
        string displayName = id.ToString();
        var def = UpgradeStateManager.Instance?.GetDef(id);
        if (def != null && def.name != null) displayName = def.name;

        string msg = newLevel > oldLevel
            ? $"{displayName} upgraded to Lv {newLevel}"
            : $"{displayName} downgraded to Lv {newLevel}";

        PlayToast(msg);
    }

    private void PlayToast(string message)
    {
        if (!toastGroup || !toastText) return;

        toastText.text = message;

        if (_playing != null) StopCoroutine(_playing);
        _playing = StartCoroutine(CoToast());
    }

    private System.Collections.IEnumerator CoToast()
    {
        toastGroup.gameObject.SetActive(true);

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            toastGroup.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }
        toastGroup.alpha = 1f;

        // Hold
        t = 0f;
        while (t < hold)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            toastGroup.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }

        toastGroup.alpha = 0f;
        toastGroup.gameObject.SetActive(false);
        _playing = null;
    }
}
