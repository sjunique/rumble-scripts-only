// ==============================
// QuestgiverTalkController.cs
// Adds human-like talk idle: head look-at, upper-body talk blend, optional visor pulse to voice.
// Works with the Animator built by QuestgiverAnimatorSetup (TalkAmount param).
// ==============================
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class QuestgiverTalkController : MonoBehaviour
{
    [Header("Refs")] public Transform playerHead;        // usually MainCamera transform
    public AudioSource voice;                              // optional voice source for glow pulse
    public Renderer visorRenderer;                         // optional: Jammo visor renderer (for emission)

    [Header("Engagement")] public float engageDistance = 6f;
    public float talkFadeIn = 0.8f, talkFadeOut = 1.2f;

    [Header("Look IK")] public float maxLookWeight = 0.9f;
    public float lookLerp = 6f;
    public Vector3 lookOffset = new(0, -0.1f, 0);
    [Range(0,1)] public float bodyIK = 0.2f, headIK = 0.7f, eyesIK = 0.5f, clampIK = 0.5f;

    [Header("Gestures")] public string talkParam = "TalkAmount";
    public Vector2 gestureChangeEvery = new(3f, 6f);
    public float treeJitter = 0.3f;

    Animator anim; int talkHash;
    float talkW, targetLookW, curLookW, nextSwap;
    Vector3 cachedLookPos;

    void Awake()
    {
        anim = GetComponent<Animator>();
        talkHash = Animator.StringToHash(talkParam);
        ScheduleNextSwap();
        if (!playerHead && Camera.main) playerHead = Camera.main.transform;
    }

    void Update()
    {
        if (!playerHead) return;
        float d = Vector3.Distance(playerHead.position, transform.position);
        bool engaging = d <= engageDistance;

        // Smooth talk weight
        float targetTalk = engaging ? 1f : 0f;
        float tSpeed = 1f / Mathf.Max(0.01f, engaging ? talkFadeIn : talkFadeOut);
        talkW = Mathf.MoveTowards(talkW, targetTalk, Time.deltaTime * tSpeed);
        anim.SetFloat(talkHash, talkW);

        // Compute look target
        Vector3 lookPos = playerHead.position + lookOffset;
        float desired = engaging ? maxLookWeight : 0f;
        curLookW = Mathf.Lerp(curLookW, desired, 1f - Mathf.Exp(-lookLerp * Time.deltaTime));
        cachedLookPos = lookPos;
        targetLookW = curLookW;

        // Jitter the talk param slightly to traverse the BlendTree children
        if (talkW > 0.5f && Time.time >= nextSwap)
        {
            float jitter = Random.Range(-treeJitter, treeJitter);
            anim.SetFloat(talkHash, Mathf.Clamp01(talkW + jitter));
            ScheduleNextSwap();
        }

        // Optional: pulse visor with audio loudness
        if (voice && visorRenderer && voice.isPlaying)
        {
            float rms = GetRMS(voice, 64);
            float glow = Mathf.Clamp01(Mathf.Lerp(0.1f, 1.0f, rms * 8f));
            foreach (var m in visorRenderer.sharedMaterials)
            {
                if (!m || !m.HasProperty("_EmissionColor")) continue;
                m.SetColor("_EmissionColor", Color.cyan * glow);
            }
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!anim || playerHead == null) return;
        anim.SetLookAtWeight(targetLookW, bodyIK, headIK, eyesIK, clampIK);
        anim.SetLookAtPosition(cachedLookPos);
    }

    void ScheduleNextSwap() => nextSwap = Time.time + Random.Range(gestureChangeEvery.x, gestureChangeEvery.y);

    float GetRMS(AudioSource src, int samples)
    {
        float[] buf = new float[samples];
        src.GetOutputData(buf, 0);
        float sum = 0f; for (int i = 0; i < samples; i++) sum += buf[i] * buf[i];
        return Mathf.Sqrt(sum / samples);
    }
}

