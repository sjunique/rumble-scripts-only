 
using System.Collections;
 
using System.Collections;
using TMPro;
using UnityEngine;

public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI subtitleText;
    public GameObject subtitlePanel;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public float subtitleDuration = 3f;
    public float typewriterSpeed = 0.03f;
    public float fadeDuration = 0.3f;

    private Coroutine typeCoroutine;
    private float timer = 0f;
    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    void Update()
    {
        if (isShowing && timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                StartCoroutine(FadeOut());
        }
    }

    // Overload for dialogue with speaker name
    public static void Show(string speaker, string subtitle, float duration = -1)
    {
        if (Instance == null) return;

        if (Instance.typeCoroutine != null)
            Instance.StopCoroutine(Instance.typeCoroutine);

        Instance.typeCoroutine = Instance.StartCoroutine(
            Instance.ShowSubtitleRoutine(speaker, subtitle, duration > 0 ? duration : Instance.subtitleDuration));
    }

    // For compatibility: dialogue without speaker name
    public static void Show(string subtitle, float duration = -1)
    {
        Show("", subtitle, duration);
    }

    IEnumerator ShowSubtitleRoutine(string speaker, string subtitle, float duration)
    {
        // Speaker name
        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            speakerNameText.text = speaker;
        }

        // Fade In
        subtitlePanel.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // Typewriter
        subtitleText.text = "";
        foreach (char c in subtitle)
        {
            subtitleText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        timer = duration;
        isShowing = true;
    }

    IEnumerator FadeOut()
    {
        isShowing = false;
        if (canvasGroup != null)
        {
            float t = 0;
            float startAlpha = canvasGroup.alpha;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        subtitlePanel.SetActive(false);
        if (speakerNameText != null) speakerNameText.gameObject.SetActive(false);
    }

    public static void HideImmediate()
    {
        if (Instance == null) return;
        if (Instance.canvasGroup != null)
            Instance.canvasGroup.alpha = 0f;
        Instance.subtitlePanel.SetActive(false);
        if (Instance.speakerNameText != null) Instance.speakerNameText.gameObject.SetActive(false);
        Instance.isShowing = false;
        Instance.timer = 0f;
    }
}

// public class SubtitleUI : MonoBehaviour
// {
//     public static SubtitleUI Instance;

//     [Header("UI References")]
//     public TextMeshProUGUI subtitleText;
//     public GameObject subtitlePanel;           // The main panel (background)
//     public CanvasGroup canvasGroup;            // For fade in/out

//     [Header("Settings")]
//     public float subtitleDuration = 3f;
//     public float typewriterSpeed = 0.03f;
//     public float fadeDuration = 0.3f;

//     private Coroutine typeCoroutine;
//     private float timer = 0f;
//     private bool isShowing = false;

//     void Awake()
//     {
//         if (Instance == null)
//             Instance = this;
//         else
//             Destroy(gameObject);

//         if (canvasGroup == null)
//             canvasGroup = GetComponent<CanvasGroup>();
//         HideImmediate();
//     }

//     void Update()
//     {
//         if (isShowing && timer > 0f)
//         {
//             timer -= Time.deltaTime;
//             if (timer <= 0f)
//                 StartCoroutine(FadeOut());
//         }
//     }

//     public static void Show(string subtitle, float duration = -1)
//     {
//         if (Instance == null) return;

//         if (Instance.typeCoroutine != null)
//             Instance.StopCoroutine(Instance.typeCoroutine);

//         Instance.typeCoroutine = Instance.StartCoroutine(
//             Instance.ShowSubtitleRoutine(subtitle, duration > 0 ? duration : Instance.subtitleDuration));
//     }

//     IEnumerator ShowSubtitleRoutine(string subtitle, float duration)
//     {
//         // Fade In
//         subtitlePanel.SetActive(true);
//         if (canvasGroup != null)
//         {
//             canvasGroup.alpha = 0f;
//             float t = 0;
//             while (t < fadeDuration)
//             {
//                 t += Time.deltaTime;
//                 canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
//                 yield return null;
//             }
//             canvasGroup.alpha = 1f;
//         }

//         // Typewriter
//         subtitleText.text = "";
//         foreach (char c in subtitle)
//         {
//             subtitleText.text += c;
//             yield return new WaitForSeconds(typewriterSpeed);
//         }

//         timer = duration;
//         isShowing = true;
//     }

//     IEnumerator FadeOut()
//     {
//         isShowing = false;
//         if (canvasGroup != null)
//         {
//             float t = 0;
//             float startAlpha = canvasGroup.alpha;
//             while (t < fadeDuration)
//             {
//                 t += Time.deltaTime;
//                 canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t / fadeDuration);
//                 yield return null;
//             }
//             canvasGroup.alpha = 0f;
//         }
//         subtitlePanel.SetActive(false);
//     }

//     public static void HideImmediate()
//     {
//         if (Instance == null) return;
//         if (Instance.canvasGroup != null)
//             Instance.canvasGroup.alpha = 0f;
//         Instance.subtitlePanel.SetActive(false);
//         Instance.isShowing = false;
//         Instance.timer = 0f;
//     }
// }


/*
public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance; // Singleton

    [Header("UI References")]
    public TextMeshProUGUI subtitleText;
    public GameObject subtitlePanel; // (optional) can be set to parent panel for show/hide

    [Header("Settings")]
    public float subtitleDuration = 3f;

    private float timer = 0f;
    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Hide at start
        Hide();
    }

    void Update()
    {
        if (isShowing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                Hide();
        }
    }

    public static void Show(string subtitle, float duration = -1)
    {
        if (Instance == null) return;

        Instance.subtitleText.text = subtitle;
        if (Instance.subtitlePanel != null)
            Instance.subtitlePanel.SetActive(true);
        else
            Instance.subtitleText.gameObject.SetActive(true);

        Instance.timer = duration > 0 ? duration : Instance.subtitleDuration;
        Instance.isShowing = true;
    }

    public static void Hide()
    {
        if (Instance == null) return;

        if (Instance.subtitlePanel != null)
            Instance.subtitlePanel.SetActive(false);
        else
            Instance.subtitleText.gameObject.SetActive(false);

        Instance.isShowing = false;
    }
}
*/
