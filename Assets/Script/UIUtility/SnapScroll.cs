using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SnapScroll : MonoBehaviour, IEndDragHandler
{
    public ScrollRect scrollRect;

    [Header("Page")]
    public int pageCount = 5;
    public float snapSpeed = 10f;

    [Header("Toggle")]
    public Toggle[] pageToggles;

    private float targetPos;
    private bool isSnapping;
    private bool isChangingToggle;

    void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (pageToggles != null && pageToggles.Length > 0)
            pageCount = pageToggles.Length;

        targetPos = scrollRect.horizontalNormalizedPosition;

        for (int i = 0; i < pageToggles.Length; i++)
        {
            int index = i;

            pageToggles[i].onValueChanged.AddListener((isOn) =>
            {
                if (isOn && !isChangingToggle)
                {
                    MoveToPage(index);
                }
            });
        }

        MoveToPage(0);
        scrollRect.horizontalNormalizedPosition = targetPos;
        isSnapping = false;
    }

    void Update()
    {
        if (!isSnapping)
            return;

        scrollRect.horizontalNormalizedPosition =
            Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition,
                targetPos,
                Time.unscaledDeltaTime * snapSpeed
            );

        if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - targetPos) < 0.001f)
        {
            scrollRect.horizontalNormalizedPosition = targetPos;
            isSnapping = false;

            UpdateToggle(GetCurrentPage());
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        MoveToPage(GetCurrentPage());
    }

    public void MoveToPage(int pageIndex)
    {
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);

        float pageSize = 1f / (pageCount - 1);
        targetPos = pageIndex * pageSize;

        if (scrollRect != null)
            scrollRect.StopMovement();

        isSnapping = true;

        UpdateToggle(pageIndex);
    }

    int GetCurrentPage()
    {
        if (pageCount <= 1)
            return 0;

        float pageSize = 1f / (pageCount - 1);
        return Mathf.RoundToInt(scrollRect.horizontalNormalizedPosition / pageSize);
    }

    void UpdateToggle(int pageIndex)
    {
        if (pageToggles == null || pageToggles.Length == 0)
            return;

        isChangingToggle = true;

        for (int i = 0; i < pageToggles.Length; i++)
        {
            pageToggles[i].isOn = i == pageIndex;
        }

        pageToggles[pageIndex].Select();

        isChangingToggle = false;
    }

    public void NextPage()
    {
        MoveToPage(GetCurrentPage() + 1);
    }

    public void PrevPage()
    {
        MoveToPage(GetCurrentPage() - 1);
    }
}