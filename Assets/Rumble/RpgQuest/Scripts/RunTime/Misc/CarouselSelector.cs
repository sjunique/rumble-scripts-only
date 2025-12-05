// Assets/Rumble/RpgQuest/Scripts/RunTime/Misc/CarouselSelector.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RpgQuest
{
    [DisallowMultipleComponent]
    public class CarouselSelector : MonoBehaviour
    {
        [Serializable]
        public class Option
        {
            public string id = "option-id";
            public string displayName = "Option";
            public Sprite image;              // for 2D preview (UI Image)
            public GameObject prefab;         // for 3D preview (PreviewTurntable)
            public Vector3 previewOffset = Vector3.zero;
            public float previewScale = 1f;
        }

        [Header("UI")]
        public Button prevButton;
        public Button nextButton;
        public Text nameLabel;
        public Image previewImage;             // optional
        public PreviewTurntable preview3D;      // optional (RawImage + RT in scene)

        [Header("Data")]
        public List<Option> options = new();
        public int startIndex = 0;

        [Header("Events")]
        public UnityEvent<int, Option> onSelectionChanged;

        public int Index { get; private set; }
        public Option Current => (options != null && options.Count > 0 && Index >= 0 && Index < options.Count) ? options[Index] : null;
void Awake()
{
    if (prevButton) prevButton.onClick.AddListener(Prev);
    if (nextButton) nextButton.onClick.AddListener(Next);

    if (options == null || options.Count == 0)
    {
        Index = 0;
        Apply();
        return;
    }

    Index = Mathf.Clamp(startIndex, 0, options.Count - 1);
    Apply();
}

        public void Next()
        {
            if (options == null || options.Count == 0) return;
            Index = (Index + 1) % options.Count;
            Apply();
        }

        public void Prev()
        {
            if (options == null || options.Count == 0) return;
            Index = (Index - 1 + options.Count) % options.Count;
            Apply();
        }

        public void SetIndex(int index, bool invokeEvent = true)
        {
            if (options == null || options.Count == 0) return;
            Index = Mathf.Clamp(index, 0, options.Count - 1);
            Apply(invokeEvent);
        }

        void Apply(bool invokeEvent = true)
        {
            var opt = Current;

            if (nameLabel)
                nameLabel.text = (opt != null ? opt.displayName : "-");

            if (previewImage)
            {
                bool hasSprite = (opt != null && opt.image != null);
                previewImage.enabled = hasSprite;
                previewImage.sprite = hasSprite ? opt.image : null;
            }

            if (preview3D)
            {
                if (opt != null && opt.prefab != null)
                    preview3D.Show(opt.prefab, opt.previewOffset, opt.previewScale);
                else
                    preview3D.Clear();
            }

            if (invokeEvent)
                onSelectionChanged?.Invoke(Index, opt);
        }

    }
}
