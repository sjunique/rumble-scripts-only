using UnityEngine;
using UnityEngine.UI;
using TMPro;  // if you don't use TMP, you can remove this
using System.Collections;
using Invector.vCharacterController;   // only if you use Invector



public class DialogueOfferUI : MonoBehaviour
{
    [Header("Panel + Text")]
    public GameObject panel;
    public TMP_Text titleTMP;
    public TMP_Text bodyTMP;
    public Text titleLegacy;
    public Text bodyLegacy;

    [Header("Buttons")]
    public Button acceptBtn;
    public Button declineBtn;


    QuestGiverStartQuest _giver;



    [Header("Gameplay integration")]
    public bool controlCursor = true;
    public bool freezePlayer = true;

    public vThirdPersonInput invectorInput;
    public vThirdPersonController invectorController;

    Rigidbody _rb;
    CapsuleCollider _col;
    Animator _anim;

    // saved states
    CursorLockMode _prevLock;
    bool _prevVisible;
    bool _prevInvInputEnabled, _prevLockMove, _prevLockRot;
    bool _prevRBKinematic;


    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (acceptBtn) acceptBtn.onClick.AddListener(Accept);
        if (declineBtn) declineBtn.onClick.AddListener(Close);
    }

    public void Open(QuestGiverStartQuest giver, string title = "Quest", string body = "Accept this quest?")
    {
        _giver = giver;

        if (titleTMP) titleTMP.text = title;
        if (bodyTMP) bodyTMP.text = body;
        if (titleLegacy) titleLegacy.text = title;
        if (bodyLegacy) bodyLegacy.text = body;

        if (panel) panel.SetActive(true);

        // --- show mouse + stop player ---
        if (controlCursor)
        {
            _prevLock = Cursor.lockState; _prevVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        if (freezePlayer)
        {
            if (!invectorController) invectorController = FindObjectOfType<vThirdPersonController>(true);
            if (!invectorInput) invectorInput = FindObjectOfType<vThirdPersonInput>(true);

            if (invectorInput) { _prevInvInputEnabled = invectorInput.enabled; invectorInput.enabled = false; }
            if (invectorController)
            {
                _prevLockMove = invectorController.lockMovement;
                _prevLockRot = invectorController.lockRotation;
                invectorController.lockMovement = true;
                invectorController.lockRotation = true;

                _rb = invectorController.GetComponent<Rigidbody>();
                _col = invectorController.GetComponent<CapsuleCollider>();
                _anim = invectorController.animator;

                if (_rb)
                {
                    _prevRBKinematic = _rb.isKinematic;
#if UNITY_6000_0_OR_NEWER
                    _rb.linearVelocity  = Vector3.zero;
#else
                    _rb.velocity = Vector3.zero;
#endif
                    _rb.angularVelocity = Vector3.zero;
                    _rb.isKinematic = true;     // <- HARD FREEZE: prevents falling
                }
                if (_col) _col.enabled = true;   // make sure collider stays on
                if (_anim) _anim.applyRootMotion = false; // optional extra safety
            }
        }
    }

    public void Accept()
    {
        Debug.Log("[DialogueUI] Accept clicked.");
        try
        {
            _giver?.AcceptQuest();   // enables FirstRouteTrigger
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DialogueUI] AcceptQuest exception: " + ex);
        }
        StartCoroutine(CloseNextFrame());
    }

    public void Close()
    {
        Debug.Log("[DialogueUI] Close called.");
        StartCoroutine(CloseNextFrame());
    }

    IEnumerator CloseNextFrame()
    {
        // Let the UI event complete and any GameObjects enabled by Accept spawn/enable
        yield return null;
        ReallyClose();
    }



    void ReallyClose()
    {
        if (panel) panel.SetActive(false);
        _giver = null;

        // --- restore player + cursor (mirrors your Open) ---
        if (freezePlayer && invectorController)
        {
            if (_anim) _anim.applyRootMotion = true;
            if (_rb) _rb.isKinematic = _prevRBKinematic;
            invectorController.lockMovement = _prevLockMove;
            invectorController.lockRotation = _prevLockRot;
            if (invectorInput) invectorInput.enabled = _prevInvInputEnabled;
        }

        if (controlCursor)
        {
            Cursor.lockState = _prevLock;
            Cursor.visible = _prevVisible;
        }

        Debug.Log("[DialogueUI] Closed and restored controls.");
    }
    void Update()
    {
        // Safety: keep cursor visible while panel is open (some controllers relock each frame)

        if (panel && panel.activeSelf && controlCursor)
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        if (panel && panel.activeSelf && controlCursor)
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }

        // Keyboard shortcuts (optional)
        if (panel && panel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E)) Accept();
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }
    }





}

 
