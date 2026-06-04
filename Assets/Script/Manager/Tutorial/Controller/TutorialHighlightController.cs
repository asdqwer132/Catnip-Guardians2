using UnityEngine;

public class TutorialHighlightController : MonoBehaviour
{
    [Header("Highlight UI")]
    public RectTransform highlightRect;

    [Header("Targets")]
    public TutorialHighlightTarget[] targets;

    [Header("Option")]
    public bool hideOnAwake = true;
    public bool followTarget = true;

    private TutorialHighlightTarget currentTarget;

    private void Awake()
    {
        if (hideOnAwake)
            HideHighlight();
    }

    private void LateUpdate()
    {
        if (!followTarget)
            return;

        if (currentTarget == null)
            return;

        UpdateHighlight();
    }

    public void ShowHighlight(int index)
    {
        TutorialHighlightTarget target = GetTarget(index);

        if (target == null)
        {
            HideHighlight();
            return;
        }

        currentTarget = target;

        if (highlightRect != null)
            highlightRect.gameObject.SetActive(true);

        UpdateHighlight();
    }

    public void HideHighlight()
    {
        currentTarget = null;

        if (highlightRect != null)
            highlightRect.gameObject.SetActive(false);
    }

    private void UpdateHighlight()
    {
        if (highlightRect == null || currentTarget == null)
            return;

        RectTransform targetRect = currentTarget.targetRect;

        if (targetRect == null)
            return;

        highlightRect.position = targetRect.position;

        if (currentTarget.useSizeOverride)
        {
            highlightRect.sizeDelta = currentTarget.sizeOverride;
        }
        else
        {
            highlightRect.sizeDelta =
                targetRect.rect.size + currentTarget.padding;
        }
    }

    private TutorialHighlightTarget GetTarget(int index)
    {
        if (targets == null)
            return null;

        if (index < 0 || index >= targets.Length)
            return null;

        return targets[index];
    }
}