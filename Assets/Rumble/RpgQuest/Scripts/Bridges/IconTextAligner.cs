using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways] // Makes it work in edit mode too
public class IconTextAligner : MonoBehaviour
{
    [Header("References")]
    public RectTransform icon;
    public RectTransform textLabel;

    [Header("Alignment Settings")]
    public bool autoUpdate = true;
    public VerticalAlignment verticalAlignment = VerticalAlignment.Middle;
    public float customYOffset = 0f;
    public bool matchHeight = false;

    public enum VerticalAlignment
    {
        Top,
        Middle,
        Bottom,
        Custom
    }

    private void Reset()
    {
        // Try to auto-detect icon and text
        HorizontalLayoutGroup layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null && transform.childCount >= 2)
        {
            // Look for an image as icon and text as label
            foreach (Transform child in transform)
            {
                if (icon == null && (child.GetComponent<Image>() != null || child.GetComponent<UnityEngine.UI.RawImage>() != null))
                {
                    icon = child.GetComponent<RectTransform>();
                }

                if (textLabel == null && (child.GetComponent<Text>() != null || child.GetComponent<TMPro.TMP_Text>() != null))
                {
                    textLabel = child.GetComponent<RectTransform>();
                }
            }
        }
    }

    void Start()
    {
        AlignChildren();
    }

    void Update()
    {
        if (autoUpdate && Application.isEditor && !Application.isPlaying)
        {
            AlignChildren();
        }
    }

    public void AlignChildren()
    {
        if (icon == null || textLabel == null) return;

        // Set pivots and anchors for vertical center alignment
        SetVerticalPivotAndAnchors(icon);
        SetVerticalPivotAndAnchors(textLabel);

        // Match heights if requested
        if (matchHeight)
        {
            MatchHeights();
        }

        // Apply vertical alignment
        ApplyVerticalAlignment();
    }

    private void SetVerticalPivotAndAnchors(RectTransform rt)
    {
        rt.pivot = new Vector2(rt.pivot.x, 0.5f);
        rt.anchorMin = new Vector2(rt.anchorMin.x, 0.5f);
        rt.anchorMax = new Vector2(rt.anchorMax.x, 0.5f);
    }

    private void MatchHeights()
    {
        // Use the larger height as target
        float targetHeight = Mathf.Max(icon.rect.height, textLabel.rect.height);

        icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        textLabel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    private void ApplyVerticalAlignment()
    {
        switch (verticalAlignment)
        {
            case VerticalAlignment.Top:
                SetLocalYPosition(icon, icon.rect.height / 2f);
                SetLocalYPosition(textLabel, textLabel.rect.height / 2f);
                break;

            case VerticalAlignment.Middle:
                SetLocalYPosition(icon, 0f);
                SetLocalYPosition(textLabel, 0f);
                break;

            case VerticalAlignment.Bottom:
                SetLocalYPosition(icon, -icon.rect.height / 2f);
                SetLocalYPosition(textLabel, -textLabel.rect.height / 2f);
                break;

            case VerticalAlignment.Custom:
                SetLocalYPosition(icon, customYOffset);
                SetLocalYPosition(textLabel, customYOffset);
                break;
        }
    }

    private void SetLocalYPosition(RectTransform rt, float yPos)
    {
        Vector3 localPos = rt.localPosition;
        localPos.y = yPos;
        rt.localPosition = localPos;
    }

    // Editor button to manually align
    [ContextMenu("Align Now")]
    public void AlignNow()
    {
        AlignChildren();
        Debug.Log("Aligned icon and text");
    }
}
    