// Copyright (c) 2016 Unity Technologies. MIT
// #NVJOB Simple Boids base by Nicholas Veselov (MIT). Refactored & extended.
//
// EnhancedBoids v2.0
// - Player influence (attract / avoid / orbit)
// - Classic boid rules: cohesion, alignment, separation
// - Behavior patterns: School, OrbitPlayer, FollowLeader, CirclePoint, Roam
// - Obstacle avoidance (spherecast) + soft bounds (BoxCollider)
// - Vertical wave / soaring, rotation clamp
// - Lightweight spatial audio (random one-shots / loop)
// - Works for fish / birds / butterflies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

[HelpURL("https://nvjob.github.io/unity/nvjob-boids")]
[AddComponentMenu("Boids/Enhanced Boids")]
public class EnhancedBoids : MonoBehaviour
{

[Header("Player Autodetect")]
[Tooltip("If true, the script will try to find/assign a player automatically.")]
public bool autoFindPlayer = true;

[Tooltip("Primary tag to look for. Leave as Player if you use Unity's default.")]
public string playerTag = "Player";

[Tooltip("Re-try interval (seconds) while searching for a player at runtime.")]
[Range(0.1f, 10f)] public float playerRetryInterval = 1.0f;

[Tooltip("Keep watching for player replacement (e.g., after respawn) and re-bind automatically.")]
public bool keepTrackingPlayer = true;

[Tooltip("Optional extra name filters to help discovery if tag not set.")]
public string[] playerNameHints = new[] { "Player", "Eve", "vBasicController", "Hero", "Character" };



    // ---------- General ----------
    [Header("General")]
    [Tooltip("Random seconds between big state shuffles.")]
    public Vector2 behavioralChangeEvery = new Vector2(2.0f, 6.0f);
    public bool debug = false;

    [Header("Patterns")]
    public BehaviorPattern pattern = BehaviorPattern.School;
    public Transform circlePoint;
    public float circleRadius = 10f;
    public float circleAngularSpeed = 30f;

    public Transform leader; // optional (FollowLeader pattern)

    public enum BehaviorPattern { School, OrbitPlayer, FollowLeader, CirclePoint, Roam }

    // ---------- Flock ----------
    [Header("Flock Anchors")]
    [Range(1, 150)] public int flockCount = 2;
    [Range(0, 5000)] public int fragmentedFlock = 30;
    [Range(0, 1)] public float fragmentedFlockYLimit = 0.5f;
    [Range(0, 1.0f)] public float migrationFrequency = 0.1f;
    [Range(0, 1.0f)] public float posChangeFrequency = 0.5f;
    [Range(0, 100)] public float smoothAnchorFollow = 0.5f;

    // ---------- Agents ----------
    [Header("Agents")]
    public GameObject birdPref;
    [Range(1, 9999)] public int agentCount = 32;

    [Tooltip("Base speed multiplier for all agents.")]
    [Range(0, 150)] public float baseSpeed = 4f;

    [Tooltip("Random per-agent speed range added on top of Base Speed.")]
    public Vector2 perAgentSpeed = new Vector2(0.5f, 2.0f);

    [Range(0, 100)] public int fragmentedAgents = 10;
    [Range(0, 1)] public float fragmentedAgentsYLimit = 1;
    [Range(0, 10)] public float soaring = 0.5f;

    [Tooltip("Vertical wave magnitude; set small for fish/butterflies.")]
    [Range(0.01f, 500)] public float verticalWave = 20;

    public bool rotationClamp = false;
    [Range(0, 360)] public float rotationClampValue = 50;
    public Vector2 scaleRandom = new Vector2(0.8f, 1.5f);

    [Header("Classic Boid Forces")]
    [Tooltip("How far each agent considers neighbors.")]
    public float neighborRadius = 5f;
    [Tooltip("How strongly agents try to keep distance.")]
    public float separationRadius = 1.5f;
    public float cohesionWeight = 0.75f;
    public float alignmentWeight = 0.6f;
    public float separationWeight = 1.25f;
    [Tooltip("How quickly forward changes are applied.")]
    public float turnRate = 4f;

    [Header("Player Influence")]
    public Transform player;
    public PlayerMode playerMode = PlayerMode.Ignore;
    public float playerInfluenceRadius = 12f;
    public float playerInfluenceWeight = 1.0f;
    public float orbitRadius = 8f;   // for OrbitPlayer
    public float orbitHeight = 0f;   // useful for birds/butterflies to stay above player
    public enum PlayerMode { Ignore, Attract, Avoid, Orbit }

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;
    public float avoidProbeRadius = 0.25f;
    public float avoidProbeDistance = 3.0f;
    public float avoidWeight = 2.5f;

    [Header("Bounds (Optional)")]
    [Tooltip("If provided, agents stay within this BoxCollider. Otherwise free-range.")]
    public BoxCollider softBounds;
    public float boundsReturnWeight = 1.25f;

    [Header("Danger Pulse (legacy compat)")]
    public bool danger = false;
    public float dangerRadius = 15;
    public float dangerSpeed = 1.5f;
    public float dangerSoaring = 0.5f;
    public LayerMask dangerLayer;

    [Header("Audio (Optional)")]
    [Tooltip("Random one-shots played by agents. Think chirps, flaps, bubbles.")]
    public AudioClip[] randomClips;
    [Tooltip("If set, agents also get a looping ambience (very low volume).")]
    public AudioClip loopClip;
    [Range(0f, 1f)] public float loopVolume = 0.05f;
    [Range(0f, 1f)] public float oneShotVolume = 0.2f;
    public Vector2 oneShotEverySeconds = new Vector2(4f, 12f);
    public Vector2 pitchJitter = new Vector2(0.9f, 1.1f);
    public bool spatialAudio = true;

    // ---------- Info ----------
    [Header("Information")]
    public string HelpURL = "nvjob.github.io/unity/nvjob-boids";
    public string ReportAProblem = "nvjob.github.io/support";
    public string Patrons = "nvjob.github.io/patrons";

    // ---------- Internals ----------
    Transform _self, _dangerTf;
    Transform[] _agents, _anchors;
    Vector3[] _rdTargetPos, _anchorPos, _anchorVel;
    Vector3[] _velSmoothing;
    float[] _agentBaseSpeed;
    int[] _agentAnchorIndex;

    float _dangerSpeedCh = 1f, _dangerSoaringCh = 1f;
    float _time;
    static WaitForSeconds _sec1 = new WaitForSeconds(1f);

    // For audio
    class AgentAudio
    {
        public AudioSource src;
        public float nextOneShotTime;
    }
    AgentAudio[] _audios;

    void Awake()
    {
        _self = transform;

if (autoFindPlayer)
{
    StartPlayerResolveLoop();
}


        CreateAnchors();
        CreateAgents();
        StartCoroutine(PeriodicShuffle());
        StartCoroutine(DangerPulse());




    }

    void LateUpdate()
    {
        MoveAnchors();
        MoveAgents(Time.deltaTime);
    }

void OnEnable()
{
    SceneManager.activeSceneChanged += OnActiveSceneChanged;
}

void OnDisable()
{
    SceneManager.activeSceneChanged -= OnActiveSceneChanged;
}

void OnActiveSceneChanged(Scene a, Scene b)
{
    if (!autoFindPlayer) return;
    // kick a fresh resolve after scene load
    StartPlayerResolveLoop(force:true);
}




    // -------------------- Anchors --------------------
    void CreateAnchors()
    {
        _anchors = new Transform[flockCount];
        _anchorPos = new Vector3[flockCount];
        _anchorVel = new Vector3[flockCount];
        _agentAnchorIndex = new int[agentCount];

        for (int i = 0; i < flockCount; i++)
        {
            var n = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            n.SetActive(debug);
            n.name = $"FlockAnchor_{i}";
            _anchors[i] = n.transform;
            _anchors[i].position = _self.position;
            _anchors[i].parent = _self;

            var rd = Random.onUnitSphere * fragmentedFlock;
            _anchorPos[i] = new Vector3(rd.x, Mathf.Abs(rd.y * fragmentedFlockYLimit), rd.z);

            var mr = n.GetComponent<MeshRenderer>();
            if (mr) mr.enabled = debug;
        }

        if (danger)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            d.name = "DangerProbe";
            var mr = d.GetComponent<MeshRenderer>();
            if (mr) mr.enabled = debug;
            d.layer = gameObject.layer;
            _dangerTf = d.transform;
            _dangerTf.parent = _self;
        }
    }

    void MoveAnchors()
    {
        for (int i = 0; i < flockCount; i++)
        {
            _anchors[i].localPosition = Vector3.SmoothDamp(
                _anchors[i].localPosition, _anchorPos[i], ref _anchorVel[i], Mathf.Max(0.0001f, smoothAnchorFollow * 0.01f));
        }
    }

    // -------------------- Agents --------------------
    void CreateAgents()
    {
        _agents = new Transform[agentCount];
        _agentBaseSpeed = new float[agentCount];
        _rdTargetPos = new Vector3[agentCount];
        _velSmoothing = new Vector3[agentCount];
        _audios = new AgentAudio[agentCount];

        for (int i = 0; i < agentCount; i++)
        {
            var t = Instantiate(birdPref, _self).transform;
            t.name = $"Agent_{i}";
            var lpv = Random.insideUnitSphere * fragmentedAgents;
            t.localPosition = _rdTargetPos[i] = new Vector3(lpv.x, lpv.y * fragmentedAgentsYLimit, lpv.z);
            t.localScale = Vector3.one * Random.Range(scaleRandom.x, scaleRandom.y);
            t.localRotation = Quaternion.Euler(0, Random.value * 360f, 0);

            _agents[i] = t;
            _agentAnchorIndex[i] = Random.Range(0, flockCount);
            _agentBaseSpeed[i] = baseSpeed + Random.Range(perAgentSpeed.x, perAgentSpeed.y);

            // Audio setup (optional)
            var audio = new AgentAudio();
            var src = t.GetComponent<AudioSource>();
            if (!src) src = t.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = spatialAudio ? 1f : 0f;
            src.minDistance = 3f;
            src.maxDistance = 25f;
            if (loopClip)
            {
                var looper = t.gameObject.AddComponent<AudioSource>();
                looper.clip = loopClip;
                looper.volume = loopVolume;
                looper.loop = true;
                looper.spatialBlend = src.spatialBlend;
                looper.minDistance = src.minDistance;
                looper.maxDistance = src.maxDistance;
                looper.Play();
            }
            audio.src = src;
            audio.nextOneShotTime = Time.time + Random.Range(oneShotEverySeconds.x, oneShotEverySeconds.y);
            _audios[i] = audio;
        }
    }

    void MoveAgents(float dt)
    {
        _time += dt;
        var wave = Vector3.up * ((verticalWave * 0.5f) - Mathf.PingPong(_time * 0.5f, verticalWave));
        var soar = Mathf.Max(0.0001f, soaring * _dangerSoaringCh) * dt;

        for (int i = 0; i < agentCount; i++)
        {
            var tf = _agents[i];
            var speed = _agentBaseSpeed[i] * _dangerSpeedCh;

            // --- Desired direction assembly ---
            Vector3 targetPos = _anchors[_agentAnchorIndex[i]].position + _rdTargetPos[i] + wave;

            // Pattern influences
            Vector3 patternDir = Vector3.zero;
            switch (pattern)
            {
                case BehaviorPattern.OrbitPlayer:
                    if (player)
                    {
                        Vector3 toCenter = (player.position + Vector3.up * orbitHeight) - tf.position;
                        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter).normalized; // simple horizontal orbit
                        patternDir += tangent * 1.0f + toCenter.normalized * Mathf.Clamp01((orbitRadius - toCenter.magnitude) / orbitRadius);
                    }
                    break;
                case BehaviorPattern.FollowLeader:
                    if (leader) patternDir += (leader.position - tf.position).normalized;
                    break;
                case BehaviorPattern.CirclePoint:
                    if (circlePoint)
                    {
                        Vector3 toC = circlePoint.position - tf.position;
                        Vector3 tangent = Quaternion.AngleAxis(circleAngularSpeed * dt, Vector3.up) * Vector3.Cross(Vector3.up, toC).normalized;
                        patternDir += (toC.normalized + tangent).normalized;
                    }
                    break;
                case BehaviorPattern.Roam:
                    // light wander by jittering targetPos
                    targetPos += new Vector3(
                        Mathf.PerlinNoise(i * 0.37f, _time * 0.7f) - 0.5f,
                        (Mathf.PerlinNoise(i * 0.11f, _time * 0.9f) - 0.5f) * fragmentedAgentsYLimit,
                        Mathf.PerlinNoise(i * 0.73f, _time * 0.5f) - 0.5f
                    ) * 2.0f;
                    break;
                // School = default anchor + classic boids below
            }

            // Classic boid neighborhood forces
            Vector3 align = Vector3.zero;
            Vector3 cohere = Vector3.zero;
            Vector3 separate = Vector3.zero;
            int nCount = 0;

            // Simple linear neighbor scan (opt: spatial hashing if very large counts)
            for (int j = 0; j < agentCount; j++)
            {
                if (j == i) continue;
                var other = _agents[j];
                Vector3 to = other.position - tf.position;
                float d = to.magnitude;
                if (d <= neighborRadius)
                {
                    nCount++;
                    cohere += other.position;
                    align += other.forward;
                    if (d < separationRadius && d > 0.0001f)
                        separate -= to / d; // push away
                }
            }

            if (nCount > 0)
            {
                cohere = ((cohere / nCount) - tf.position).normalized;
                align = align.normalized;
                separate = separate.normalized;
            }

            Vector3 boidDir =
                cohere * cohesionWeight +
                align * alignmentWeight +
                separate * separationWeight;

            // Player influence
            if (player && playerMode != PlayerMode.Ignore)
            {
                Vector3 toP = player.position - tf.position;
                float d = toP.magnitude;
                if (d <= playerInfluenceRadius)
                {
                    Vector3 pDir = Vector3.zero;
                    switch (playerMode)
                    {
                        case PlayerMode.Attract: pDir = toP.normalized; break;
                        case PlayerMode.Avoid: pDir = (-toP).normalized; break;
                        case PlayerMode.Orbit:
                            Vector3 tangent = Vector3.Cross(Vector3.up, toP).normalized;
                            pDir = tangent + (orbitRadius > 0 ? toP.normalized * Mathf.Clamp01((orbitRadius - d) / orbitRadius) : Vector3.zero);
                            break;
                    }
                    boidDir += pDir * playerInfluenceWeight;
                }
            }

            // Direction to anchor target
            Vector3 toTarget = (targetPos - tf.position);
            Vector3 desire = toTarget.normalized + patternDir + boidDir;

            // Obstacle avoidance
            Vector3 avoid = Vector3.zero;
            if (Physics.SphereCast(tf.position, avoidProbeRadius, tf.forward, out RaycastHit hit, avoidProbeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // steer away from obstacle normal and off to side
                var reflect = Vector3.Reflect(tf.forward, hit.normal);
                var side = Vector3.Cross(hit.normal, Vector3.up).normalized;
                avoid = (reflect + side).normalized * avoidWeight;
            }
            desire += avoid;

            // Soft bounds (BoxCollider)
            if (softBounds)
            {
                Vector3 local = softBounds.transform.InverseTransformPoint(tf.position);
                Vector3 half = softBounds.size * 0.5f;
                // If outside a gentle margin, steer back in
                Vector3 nudge = Vector3.zero;
                if (Mathf.Abs(local.x) > half.x) nudge.x = -Mathf.Sign(local.x);
                if (Mathf.Abs(local.y) > half.y) nudge.y = -Mathf.Sign(local.y);
                if (Mathf.Abs(local.z) > half.z) nudge.z = -Mathf.Sign(local.z);
                if (nudge != Vector3.zero)
                    desire += softBounds.transform.TransformDirection(nudge).normalized * boundsReturnWeight;
            }

            // Apply rotation toward desire
            if (desire.sqrMagnitude > 0.0001f)
            {
                Vector3 steer = Vector3.RotateTowards(tf.forward, desire.normalized, turnRate * soar, 0);
                Quaternion rot = Quaternion.LookRotation(steer, Vector3.up);
                if (!rotationClamp) tf.rotation = rot;
                else tf.localRotation = ClampPitch(rot, rotationClampValue);
            }

            // Translate forward
            tf.Translate(Vector3.forward * speed * dt, Space.Self);

            // Audio tick
            TickAudio(i);
        }
    }

    // -------------------- Coroutines --------------------
    IEnumerator PeriodicShuffle()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(behavioralChangeEvery.x, behavioralChangeEvery.y));

            // Shuffle anchors
            for (int f = 0; f < flockCount; f++)
            {
                if (Random.value < posChangeFrequency)
                {
                    Vector3 rd = Random.insideUnitSphere * fragmentedFlock;
                    _anchorPos[f] = new Vector3(rd.x, Mathf.Abs(rd.y * fragmentedFlockYLimit), rd.z);
                }
            }

            // Shuffle agents
            for (int i = 0; i < agentCount; i++)
            {
                var speedDelta = Random.Range(perAgentSpeed.x, perAgentSpeed.y);
                _agentBaseSpeed[i] = baseSpeed + speedDelta;
                Vector3 lpv = Random.insideUnitSphere * fragmentedAgents;
                _rdTargetPos[i] = new Vector3(lpv.x, lpv.y * fragmentedAgentsYLimit, lpv.z);
                if (Random.value < migrationFrequency) _agentAnchorIndex[i] = Random.Range(0, flockCount);
            }
        }
    }

    IEnumerator DangerPulse()
    {
        if (!danger)
        {
            _dangerSpeedCh = _dangerSoaringCh = 1f;
            yield break;
        }

        while (true)
        {
            int idx = Random.Range(0, agentCount);
            if (_dangerTf) _dangerTf.localPosition = _agents[idx].localPosition;

            if (Physics.CheckSphere(_dangerTf.position, dangerRadius, dangerLayer, QueryTriggerInteraction.Ignore))
            {
                _dangerSpeedCh = dangerSpeed;
                _dangerSoaringCh = dangerSoaring;
            }
            else
            {
                _dangerSpeedCh = _dangerSoaringCh = 1f;
            }
            yield return _sec1;
        }
    }

    // -------------------- Audio --------------------
    void TickAudio(int i)
    {
        if (randomClips == null || randomClips.Length == 0) return;

        var a = _audios[i];
        if (!a?.src) return;

        if (Time.time >= a.nextOneShotTime)
        {
            var clip = randomClips[Random.Range(0, randomClips.Length)];
            a.src.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
            a.src.volume = oneShotVolume;
            a.src.PlayOneShot(clip);
            a.nextOneShotTime = Time.time + Random.Range(oneShotEverySeconds.x, oneShotEverySeconds.y);
        }
    }

    // -------------------- Utils --------------------
    static Quaternion ClampPitch(Quaternion rotationCur, float clamp)
    {
        Vector3 e = rotationCur.eulerAngles;
        e = new Vector3(
            Mathf.Clamp((e.x > 180) ? e.x - 360 : e.x, -clamp, clamp),
            e.y,
            0f
        );
        rotationCur.eulerAngles = e;
        return rotationCur;
    }

// Public hook for spawners to call as soon as a player is created.
public void AssignPlayer(Transform t)
{
    if (t == null) return;
    player = t;
#if UNITY_EDITOR
    if (debug) Debug.Log($"[EnhancedBoids] Player assigned externally: {player.name}", this);
#endif
}

// Kick/Restart resolve loop
void StartPlayerResolveLoop(bool force = false)
{
    if (!force && (_playerResolveRunning || player != null)) return;
    if (_playerResolveCo != null) StopCoroutine(_playerResolveCo);
    _playerResolveCo = StartCoroutine(Co_ResolvePlayerLoop());
}

Coroutine _playerResolveCo;
bool _playerResolveRunning = false;

IEnumerator Co_ResolvePlayerLoop()
{
    _playerResolveRunning = true;
    var wait = new WaitForSeconds(Mathf.Max(0.1f, playerRetryInterval));

    // Keep trying until we have a player; if keepTracking, keep monitoring afterward.
    while (true)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            var found = TryResolvePlayerOnce();
            if (found && player != null)
            {
#if UNITY_EDITOR
                if (debug) Debug.Log($"[EnhancedBoids] Auto-detected player: {player.name}", this);
#endif
            }
        }
        else if (!keepTrackingPlayer)
        {
            break; // we have a live player and no need to keep watching
        }
        yield return wait;
    }

    _playerResolveRunning = false;
    _playerResolveCo = null;
}

// One-shot resolve attempt
bool TryResolvePlayerOnce()
{
    Transform candidate = null;

    // 1) Tag-based
    if (!string.IsNullOrEmpty(playerTag))
    {
        var tagged = GameObject.FindGameObjectWithTag(playerTag);
        if (tagged && tagged.activeInHierarchy) candidate = tagged.transform;
    }

    // 2) Name hints
    if (candidate == null && playerNameHints != null && playerNameHints.Length > 0)
    {
        var all = FindObjectsOfType<Transform>(includeInactive: false);
        // score and pick best
        candidate = all
            .Where(t => t && t.gameObject.activeInHierarchy)
            .Select(t => new
            {
                t,
                score = ScoreCandidate(t, _self.position, playerNameHints)
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => Vector3.SqrMagnitude(x.t.position - _self.position))
            .Select(x => x.t)
            .FirstOrDefault();
    }

    // 3) Fallback: nearest Transform that looks like a controllable character
    if (candidate == null)
    {
        var all = FindObjectsOfType<Transform>(includeInactive: false);
        candidate = all
            .Where(LooksLikeAPlayer)
            .OrderBy(t => Vector3.SqrMagnitude(t.position - _self.position))
            .FirstOrDefault();
    }

    if (candidate != null)
    {
        player = candidate;
        return true;
    }

    return false;
}

static int ScoreCandidate(Transform t, Vector3 origin, string[] hints)
{
    int score = 0;
    var n = t.name;
    // Name hits
    if (!string.IsNullOrEmpty(n))
    {
        foreach (var h in hints)
        {
            if (string.IsNullOrEmpty(h)) continue;
            if (n.IndexOf(h, System.StringComparison.OrdinalIgnoreCase) >= 0) score += 2;
        }
    }
    // Tag bonus
    if (t.CompareTag("Player")) score += 3;

    // Component cues
    if (t.GetComponent<CharacterController>()) score += 2;
    if (t.GetComponent<Rigidbody>()) score += 1;

    // Distance preference (closer is likely)
    float dist = Vector3.Distance(origin, t.position);
    if (dist < 30f) score += 1;
    if (dist < 10f) score += 1;

    return score;
}

static bool LooksLikeAPlayer(Transform t)
{
    if (!t || !t.gameObject.activeInHierarchy) return false;
    if (t.CompareTag("Player")) return true;
    if (t.GetComponent<CharacterController>()) return true;
    if (t.GetComponent<Rigidbody>()) return true;

    // Weak name hints even in fallback
    string n = t.name;
    if (!string.IsNullOrEmpty(n))
    {
        if (n.IndexOf("Player", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (n.IndexOf("Character", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (n.IndexOf("Hero", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (n.IndexOf("Eve", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    }
    return false;
}




    void OnDrawGizmosSelected()
    {
        if (!debug) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fragmentedFlock);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        if (player)
        {
            Gizmos.color = playerMode == PlayerMode.Avoid ? Color.red : Color.green;
            Gizmos.DrawWireSphere(player.position, playerInfluenceRadius);
            if (playerMode == PlayerMode.Orbit)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(player.position + Vector3.up * orbitHeight, orbitRadius);
            }
        }

        if (circlePoint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(circlePoint.position, circleRadius);
        }
    }
}

