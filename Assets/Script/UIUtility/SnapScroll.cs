using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SnapScroll : MonoBehaviour, IEndDragHandler
{
    [Header("Scroll")]
    public ScrollRect scrollRect;

    [Header("Page")]
    public int pageCount = 1;
    public float snapSpeed = 10f;

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
            UpdateButtons();
            UpdatePageText();
        }
    }

    public void SetPageCount(int count)
    {
        pageCount = Mathf.Max(1, count);
        currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

        if (scrollRect != null)
        {
            scrollRect.horizontal = pageCount > 1;
            scrollRect.StopMovement();
            scrollRect.velocity = Vector2.zero;
        }

        SetTargetByPage(currentPage);
        UpdateButtons();
        UpdatePageText();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (pageCount <= 1)
            return;

        MoveToPage(GetNearestPage());
    }

    public void MoveToPage(int pageIndex)
    {
        if (scrollRect == null)
            return;

        pageIndex = GetValidPageIndex(pageIndex);

        currentPage = pageIndex;
        SetTargetByPage(currentPage);

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
        isSnapping = true;

        UpdateButtons();
        UpdatePageText();
    }

    public void MoveToPageInstant(int pageIndex)
    {
        if (scrollRect == null)
            return;

        pageIndex = GetValidPageIndex(pageIndex);

        currentPage = pageIndex;
        SetTargetByPage(currentPage);

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
        scrollRect.horizontalNormalizedPosition = targetPos;
        isSnapping = false;

        UpdateButtons();
        UpdatePageText();
    }

    public void NextPage()
    {
        if (pageCount <= 1)
            return;

        MoveToPage(currentPage + 1);
    }

    public void PrevPage()
    {
        if (pageCount <= 1)
            return;

        MoveToPage(currentPage - 1);
    }

    private int GetValidPageIndex(int pageIndex)
    {
        if (pageCount <= 1)
            return 0;

        if (!infiniteLoop)
            return Mathf.Clamp(pageIndex, 0, pageCount - 1);

        if (pageIndex < 0)
            return pageCount - 1;

        if (pageIndex >= pageCount)
            return 0;

        return pageIndex;
    }

    private void SetTargetByPage(int pageIndex)
    {
        if (pageCount <= 1)
        {
            targetPos = 0f;
            return;
        }

        float pageSize = 1f / (pageCount - 1);
        targetPos = pageIndex * pageSize;
    }

    private int GetNearestPage()
    {
        if (pageCount <= 1 || scrollRect == null)
            return 0;

        float pageSize = 1f / (pageCount - 1);

        return Mathf.Clamp(
            Mathf.RoundToInt(scrollRect.horizontalNormalizedPosition / pageSize),
            0,
            pageCount - 1
        );
    }

    private void UpdateButtons()
    {
        if (infiniteLoop)
        {
            if (prevButton != null)
                prevButton.interactable = pageCount > 1;

            if (nextButton != null)
                nextButton.interactable = pageCount > 1;

            return;
        }

        if (prevButton != null)
            prevButton.interactable = pageCount > 1 && currentPage > 0;

        if (nextButton != null)
            nextButton.interactable = pageCount > 1 && currentPage < pageCount - 1;
    }

    private void UpdatePageText()
    {
        if (pageCountText == null)
            return;

        pageCountText.text = $"{currentPage + 1}/{pageCount}";
    }

    public int GetCurrentPage()
    {
        return currentPage;
    }

    public int GetPageCount()
    {
        return pageCount;
    }
}