using UnityEngine;
using UnityEngine.UIElements;


[RequireComponent(typeof(UIDocument))]
public class JammoQuestDialogUI : MonoBehaviour
{
    [SerializeField] JammoQA_DialogBridge _bridge;

    UIDocument _doc;
    VisualElement _root;
    public bool Visible { get; private set; }

    void Awake()
    {
        _doc  = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;
        if (_bridge == null) _bridge = GetComponent<JammoQA_DialogBridge>();
        Hide();
    }

    void OnEnable()
    {
        if (_bridge != null)
        {
            _bridge.OnAcceptClicked.AddListener(HandleBridgeAccept);
            _bridge.OnDeclineClicked.AddListener(HandleBridgeDecline);
        }
    }

    void OnDisable()
    {
        if (_bridge != null)
        {
            _bridge.OnAcceptClicked.RemoveListener(HandleBridgeAccept);
            _bridge.OnDeclineClicked.RemoveListener(HandleBridgeDecline);
        }
    }

    void HandleBridgeAccept()  => Hide();
    void HandleBridgeDecline() => Hide();

    public void Show()
    {
        if (_root == null) _root = GetComponent<UIDocument>().rootVisualElement;

        EventSystemOrchestrator.I?.SuspendAllUGUI();

        _root.style.display = DisplayStyle.Flex;
        _root.pickingMode   = PickingMode.Position;
        Visible = true;

        UnityEngine.Cursor.visible   = true;
        UnityEngine. Cursor.lockState = CursorLockMode.None;
    }

    public void Hide()
    {
        if (_root == null) return;

        _root.style.display = DisplayStyle.None;
        Visible = false;

        EventSystemOrchestrator.I?.ResumeAllUGUI();

         UnityEngine.Cursor.visible   = false;
         UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (!Visible) return;

        if (Input.GetKeyDown(KeyCode.E))
            _bridge?.InvokeAccept();   // will trigger HandleBridgeAccept → Hide()

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
            _bridge?.InvokeDecline();  // will trigger HandleBridgeDecline → Hide()
    }
}



/*
[RequireComponent(typeof(UIDocument))]
public class JammoQuestDialogUI : MonoBehaviour
{
    [SerializeField] JammoQA_DialogBridge _bridge;   // assign or auto-get

    UIDocument _doc;
    VisualElement _root;
    public bool Visible { get; private set; }

    void Awake()
    {
        _doc  = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;
        if (_bridge == null) _bridge = GetComponent<JammoQA_DialogBridge>();
        Hide(); // start hidden
    }


   void OnEnable()
    {
        // subscribe to bridge events so mouse clicks also close the dialog
        if (_bridge != null)
        {
            _bridge.OnAcceptClicked.AddListener(HandleBridgeAccept);
            _bridge.OnDeclineClicked.AddListener(HandleBridgeDecline);
        }
    }

    void OnDisable()
    {
        if (_bridge != null)
        {
            _bridge.OnAcceptClicked.RemoveListener(HandleBridgeAccept);
            _bridge.OnDeclineClicked.RemoveListener(HandleBridgeDecline);
        }
    }

    void HandleBridgeAccept()
    {
        // Quest logic will run via other listeners;
        // here we only care about closing the panel.
        Hide();
    }

  void HandleBridgeDecline()
    {
        Hide();
    }


      public void Show()
    {
        if (_root == null) _root = GetComponent<UIDocument>().rootVisualElement;

        EventSystemOrchestrator.I?.SuspendAllUGUI();

        _root.style.display = DisplayStyle.Flex;
        _root.pickingMode   = PickingMode.Position;
        Visible = true;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    public void Hide()
    {
        if (_root == null) return;

        _root.style.display = DisplayStyle.None;
        Visible = false;

        EventSystemOrchestrator.I?.ResumeAllUGUI();

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }
  
   void Update()
    {
        if (!Visible) return;

        if (Input.GetKeyDown(KeyCode.E)) {
            _bridge?.InvokeAccept();   // triggers HandleBridgeAccept
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape)) {
            _bridge?.InvokeDecline();  // triggers HandleBridgeDecline
        }
    }
}
*/