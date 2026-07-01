using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SnapScroll : MonoBehaviour, IEndDragHandler
{
    [Header("Scroll")]
    public ScrollRect scrollRect;

    [Header("Page")]
    public int pageCount = 1;
    public float snapSpeed = 10f;

    [Header("Move Option")]
    public bool instantMove = false;

    [Header("Loop")]
    public bool infiniteLoop = false;

    [Header("Buttons")]
    public Button nextButton;
    public Button prevButton;

    [Header("Page Text")]
    public TMP_Text pageCountText;

    private int currentPage;
    private float targetPos;
    private bool isSnapping;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
    }

    private void Start()
    {
        SetPageCount(pageCount);
        MoveToPageInstant(0);
    }

    private void Update()
    {
        if (!isSnapping || scrollRect == null)
            return;

        scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
            scrollRect.horizontalNormalizedPosition,
            targetPos,
            Time.unscaledDeltaTime * snapSpeed
        );

        if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - targetPos) < 0.001f)
        {
            scrollRect.horizontalNormalizedPosition = targetPos;
            isSnapping = false;
        }
    }

    public void SetPageCount(int count)
    {
        pageCount = Mathf.Max(1, count);

        if (currentPage >= pageCount)
            currentPage = pageCount - 1;

        if (currentPage < 0)
            currentPage = 0;

        if (scrollRect != null)
            scrollRect.enabled = pageCount > 1;

        SetTargetByPage(currentPage);

        if (instantMove)
            MoveToPageInstant(currentPage);

        UpdateButtons();
        UpdatePageText();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (pageCount <= 1)
            return;

        int nearestPage = GetNearestPage();
        MoveToPage(nearestPage);
    }

    public void MoveToPage(int pageIndex)
    {
        pageIndex = GetValidPageIndex(pageIndex);

        currentPage = pageIndex;
        SetTargetByPage(currentPage);

        if (instantMove)
        {
            ApplyTargetInstant();
        }
        else
        {
            isSnapping = true;
        }

        UpdateButtons();
        UpdatePageText();
    }

    public void MoveToPageInstant(int pageIndex)
    {
        pageIndex = GetValidPageIndex(pageIndex);

        currentPage = pageIndex;
        SetTargetByPage(currentPage);
        ApplyTargetInstant();

        UpdateButtons();
        UpdatePageText();
    }

    public void NextPage()
    {
        MoveToPage(currentPage + 1);
    }

    public void PrevPage()
    {
        MoveToPage(currentPage - 1);
    }

    private void ApplyTargetInstant()
    {
        isSnapping = false;

        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = targetPos;
    }

    private int GetValidPageIndex(int pageIndex)
    {
        if (pageCount <= 1)
            return 0;

        if (infiniteLoop)
        {
            if (pageIndex < 0)
                return pageCount - 1;

            if (pageIndex >= pageCount)
                return 0;

            return pageIndex;
        }

        return Mathf.Clamp(pageIndex, 0, pageCount - 1);
    }

    private void SetTargetByPage(int pageIndex)
    {
        if (pageCount <= 1)
        {
            targetPos = 0f;
            return;
        }

        targetPos = (float)pageIndex / (pageCount - 1);
    }

    private int GetNearestPage()
    {
        if (scrollRect == null)
            return currentPage;

        if (pageCount <= 1)
            return 0;

        float position = scrollRect.horizontalNormalizedPosition;
        int nearestPage = Mathf.RoundToInt(position * (pageCount - 1));

        return GetValidPageIndex(nearestPage);
    }

    private void UpdateButtons()
    {
        if (pageCount <= 1)
        {
            if (prevButton != null)
                prevButton.interactable = false;

            if (nextButton != null)
                nextButton.interactable = false;

            return;
        }

        if (infiniteLoop)
        {
            if (prevButton != null)
                prevButton.interactable = true;

            if (nextButton != null)
                nextButton.interactable = true;

            return;
        }

        if (prevButton != null)
            prevButton.interactable = currentPage > 0;

        if (nextButton != null)
            nextButton.interactable = currentPage < pageCount - 1;
    }

    private void UpdatePageText()
    {
        if (pageCountText != null)
            pageCountText.text = $"{currentPage + 1}/{pageCount}";
    }
}