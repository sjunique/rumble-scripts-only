using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class QuestHud_TK : MonoBehaviour
{
    [Header("Templates")]
    public VisualTreeAsset hudDocument;       // optional: if your UIDocument already has the HUD, leave null
    public VisualTreeAsset entryTemplate;     // QuestHudEntry_TK.uxml

    [Header("Names in UXML")]
    public string hudRootName  = "quest-hud";
    public string listName     = "quest-list";
    public string rowNameLabel = "q-name";
    public string rowObjLabel  = "q-obj";

    [Header("Refresh")]
    public float refreshInterval = 0.25f;     // light polling, or call Refresh() manually after events

    UIDocument _doc;
    VisualElement _hudRoot;
    ScrollView _list;

    float _timer;
    int _lastQuestCount = -1;                 // cheap change detection
    int _lastHash = 0;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        if (hudDocument != null) _doc.visualTreeAsset = hudDocument;
    }

    void OnEnable()
    {
        var root = _doc.rootVisualElement;
        _hudRoot = root.Q<VisualElement>(hudRootName);
        _list    = root.Q<ScrollView>(listName);

        if (_hudRoot == null || _list == null)
            Debug.LogError("[QuestHud_TK] Missing 'quest-hud' or 'quest-list' in UXML.");

        Refresh(force:true);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= refreshInterval)
        {
            _timer = 0f;
            Refresh();
        }
    }

    public void Refresh(bool force = false)
    {
        var qm = QuestManager.Instance;
        if (qm == null || _list == null) return;

        var quests = qm.activeQuests;
        int count  = quests != null ? quests.Count : 0;

        // Build a small hash to avoid rebuilding every tick if nothing changed
        int h = count;
        if (quests != null)
        {
            unchecked
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    var q = quests[i];
                    h = (h * 397) ^ (q != null ? q.GetHashCode() : 0);
                    if (q?.objectives != null)
                    {
                        for (int j = 0; j < q.objectives.Length; j++)
                        {
                            var o = q.objectives[j];
                            h = (h * 397) ^ (o.currentAmount * 31 + o.requiredAmount);
                        }
                    }
                }
            }
        }

        if (!force && count == _lastQuestCount && h == _lastHash) return;

        _list.Clear();

        if (quests != null)
        {
            for (int i = 0; i < quests.Count; i++)
            {
                var q = quests[i];
                if (q == null) continue;

                // create row
                VisualElement row = entryTemplate != null
                    ? entryTemplate.Instantiate()
                    : BuildRowFallback(); // in case template missing

                // bind
                var nameLbl = row.Q<Label>(rowNameLabel);
                var objLbl  = row.Q<Label>(rowObjLabel);

                if (nameLbl != null) nameLbl.text = q.questName;

                if (objLbl != null)
                {
                    var sb = new StringBuilder();
                    if (q.objectives != null)
                    {
                        for (int j = 0; j < q.objectives.Length; j++)
                        {
                            var o = q.objectives[j];
                            sb.Append(o.objectiveName)
                              .Append(": ")
                              .Append(o.currentAmount)
                              .Append("/")
                              .Append(o.requiredAmount);
                            if (j < q.objectives.Length - 1) sb.Append("\n");
                        }
                    }
                    objLbl.text = sb.ToString();
                }

                _list.Add(row);
            }
        }

        _lastQuestCount = count;
        _lastHash = h;
    }

    // fallback row if no template assigned
    VisualElement BuildRowFallback()
    {
        var wrap = new VisualElement();
        wrap.AddToClassList("qhud-entry");

        var name = new Label() { name = rowNameLabel };
        name.AddToClassList("qhud-name");

        var obj  = new Label() { name = rowObjLabel };
        obj.AddToClassList("qhud-objs");

        wrap.Add(name);
        wrap.Add(obj);
        return wrap;
    }
}
