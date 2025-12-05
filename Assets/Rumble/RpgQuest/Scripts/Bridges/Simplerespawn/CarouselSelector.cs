using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CarouselSelector : MonoBehaviour
{
    [Serializable]
    public class Option
    {
        public string displayName;
        public GameObject prefab;
        public Sprite icon;
    }

    [Header("UI")]
    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Options")]
    [SerializeField] private Option[] options;
    [SerializeField] private int startIndex = 0;

    public event Action<Option> OnChanged;

    private int _index = 0;
    public int CurrentIndex => _index;
    public Option Current => (options != null && options.Length > 0) ? options[_index] : null;

    private void Awake()
    {
        if (prevBtn) prevBtn.onClick.AddListener(Prev);
        if (nextBtn) nextBtn.onClick.AddListener(Next);

        if (options == null) options = Array.Empty<Option>();
        _index = Wrap(startIndex, options.Length);
        Refresh();
    }

    public void SetOptions(Option[] newOptions, int initialIndex = 0)
    {
        options = newOptions ?? Array.Empty<Option>();
        _index = Wrap(initialIndex, options.Length);
        Refresh();
    }

    public void Next() => SelectIndex(_index + 1);
    public void Prev() => SelectIndex(_index - 1);

    public void SelectIndex(int idx)
    {
        if (options == null || options.Length == 0) return;
        int newIndex = Wrap(idx, options.Length);
        if (newIndex == _index) return;
        _index = newIndex;
        Refresh();
    }

    private void Refresh()
    {
        var opt = Current;

        if (label) label.text = opt != null ? opt.displayName : "";

        if (iconImage)
        {
            iconImage.enabled = (opt != null && opt.icon != null);
            iconImage.sprite  = opt != null ? opt.icon : null;
        }

        OnChanged?.Invoke(opt);
    }

    private static int Wrap(int idx, int count)
    {
        if (count <= 0) return 0;
        int m = idx % count;
        if (m < 0) m += count;
        return m;
    }
}
