using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPurchaseResultPanelUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;
    public CanvasGroup panelCanvasGroup;

    [Header("Title")]
    public TextMeshProUGUI titleText;
    public string defaultTitle = "획득 결과";

    [Header("Result List")]
    public Transform itemListParent;
    public ShopPurchaseResultSlotUI resultSlotPrefab;

    [Header("Confirm")]
    public Button confirmButton;

    [Header("Open Option")]
    public bool hideOnAwake = true;
    public bool clearPreviousOnShow = true;

    [Header("List Rise Effect")]
    public bool useListRiseEffect = true;
    public float riseStartYOffset = -60f;
    public float riseDuration = 0.25f;
    public bool fadeListWithRise = true;
    public bool useUnscaledTime = true;

    private RectTransform itemListRect;
    private CanvasGroup itemListCanvasGroup;
    private Vector2 itemListOriginPosition;
    private Coroutine effectRoutine;

    private readonly List<GameObject> spawnedSlots = new List<GameObject>();

    private void Reset()
    {
        panelRoot = gameObject;
        panelCanvasGroup = GetComponent<CanvasGroup>();
        confirmButton = GetComponentInChildren<Button>();
    }

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Hide);

        CacheListComponents();

        if (hideOnAwake)
            HideImmediate();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Hide);
    }

    private void CacheListComponents()
    {
        if (itemListParent == null)
            return;

        itemListRect = itemListParent as RectTransform;

        if (itemListRect != null)
            itemListOriginPosition = itemListRect.anchoredPosition;

        itemListCanvasGroup = itemListParent.GetComponent<CanvasGroup>();
        if (itemListCanvasGroup == null)
            itemListCanvasGroup = itemListParent.gameObject.AddComponent<CanvasGroup>();
    }

    public void ShowResult(ItemData itemData, int count = 1)
    {
        List<ShopPurchaseResultEntry> results = new List<ShopPurchaseResultEntry>();

        if (itemData != null)
            results.Add(new ShopPurchaseResultEntry(itemData, count));

        ShowResults(results);
    }

    public void ShowResults(List<ItemData> itemDatas)
    {
        List<ShopPurchaseResultEntry> results = BuildEntries(itemDatas);
        ShowResults(results);
    }

    public void ShowResults(List<ShopPurchaseResultEntry> results)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        if (titleText != null)
            titleText.text = defaultTitle;

        if (clearPreviousOnShow)
            ClearSlots();

        CreateSlots(results);

        if (effectRoutine != null)
            StopCoroutine(effectRoutine);

        if (useListRiseEffect)
            effectRoutine = StartCoroutine(ListRiseRoutine());
        else
            ResetListEffectState();
    }

    public void Hide()
    {
        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        ResetListEffectState();

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void CreateSlots(List<ShopPurchaseResultEntry> results)
    {
        if (itemListParent == null || resultSlotPrefab == null || results == null)
            return;

        for (int i = 0; i < results.Count; i++)
        {
            ShopPurchaseResultEntry result = results[i];
            if (result.itemData == null || result.count <= 0)
                continue;

            ShopPurchaseResultSlotUI slot = Instantiate(resultSlotPrefab, itemListParent);
            slot.SetSlot(result.itemData, result.count);
            spawnedSlots.Add(slot.gameObject);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemListParent as RectTransform);
    }

    private List<ShopPurchaseResultEntry> BuildEntries(List<ItemData> itemDatas)
    {
        List<ShopPurchaseResultEntry> results = new List<ShopPurchaseResultEntry>();

        if (itemDatas == null)
            return results;

        for (int i = 0; i < itemDatas.Count; i++)
        {
            ItemData itemData = itemDatas[i];
            if (itemData == null)
                continue;

            int index = FindEntryIndex(results, itemData);
            if (index >= 0)
            {
                ShopPurchaseResultEntry entry = results[index];
                entry.count += 1;
                results[index] = entry;
            }
            else
            {
                results.Add(new ShopPurchaseResultEntry(itemData, 1));
            }
        }

        return results;
    }

    private int FindEntryIndex(List<ShopPurchaseResultEntry> results, ItemData itemData)
    {
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].itemData == itemData)
                return i;
        }

        return -1;
    }

    private void ClearSlots()
    {
        for (int i = spawnedSlots.Count - 1; i >= 0; i--)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i]);
        }

        spawnedSlots.Clear();

        if (itemListParent == null)
            return;

        for (int i = itemListParent.childCount - 1; i >= 0; i--)
            Destroy(itemListParent.GetChild(i).gameObject);
    }

    private IEnumerator ListRiseRoutine()
    {
        CacheListComponents();

        if (itemListRect == null)
            yield break;

        Vector2 startPosition = itemListOriginPosition + new Vector2(0f, riseStartYOffset);
        Vector2 endPosition = itemListOriginPosition;

        itemListRect.anchoredPosition = startPosition;

        if (itemListCanvasGroup != null)
            itemListCanvasGroup.alpha = fadeListWithRise ? 0f : 1f;

        float duration = Mathf.Max(0.01f, riseDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += DeltaTime();
            float t = Mathf.Clamp01(timer / duration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            itemListRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, smoothT);

            if (itemListCanvasGroup != null && fadeListWithRise)
                itemListCanvasGroup.alpha = t;

            yield return null;
        }

        ResetListEffectState();
        effectRoutine = null;
    }

    private void ResetListEffectState()
    {
        CacheListComponents();

        if (itemListRect != null)
            itemListRect.anchoredPosition = itemListOriginPosition;

        if (itemListCanvasGroup != null)
            itemListCanvasGroup.alpha = 1f;
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
