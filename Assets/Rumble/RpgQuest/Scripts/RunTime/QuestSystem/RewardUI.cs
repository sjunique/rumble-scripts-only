using UnityEngine;
using UnityEngine.UI;

public class RewardUI : MonoBehaviour
{
    public static RewardUI Instance { get; private set; }

    [Header("Icons")]
    public Image shieldIcon;
    public Image scubaIcon;

    [Header("Toast (optional)")]
    public Text toastText;
    public float toastSeconds = 1.5f;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // enable if you travel scenes
        ShowShieldIcon(false);
        ShowScubaIcon(false);
        if (toastText) toastText.gameObject.SetActive(false);
    }

    public void ShowShieldIcon(bool on) { if (shieldIcon) shieldIcon.enabled = on; }
    public void ShowScubaIcon(bool on)  { if (scubaIcon) scubaIcon.enabled = on; }

    public void Toast(string msg)
    {
        if (!toastText) return;
        StopAllCoroutines();
        StartCoroutine(ToastRoutine(msg));
    }

    System.Collections.IEnumerator ToastRoutine(string msg)
    {
        toastText.text = msg;
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(toastSeconds);
        toastText.gameObject.SetActive(false);
    }
}

