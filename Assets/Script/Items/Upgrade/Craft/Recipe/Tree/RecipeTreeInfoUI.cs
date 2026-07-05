using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RecipeTreeInfo
{
    public ItemSeries itemSeries;
    public Toggle toggle;
    public GameObject panel;
}

public class RecipeTreeInfoUI : MonoBehaviour
{
    [Header("Manager")]
    public ItemCombinationManager itemCombinationManager;

    [Header("Info")]
    public TextMeshProUGUI totalNodeText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI craftedText;
    public TextMeshProUGUI missingText;
    public TextMeshProUGUI percentText;
    public Slider percentSlider;

    [Header("UI")]
    public RecipeTreeInfo[] recipeTreeInfos;

    private void Awake()
    {
        BindToggles();
    }

    private void Start()
    {
        InitializePanel();
    }

    private void OnDestroy()
    {
        UnbindToggles();
    }

    private void BindToggles()
    {
        if (recipeTreeInfos == null)
            return;

        for (int i = 0; i < recipeTreeInfos.Length; i++)
        {
            RecipeTreeInfo info = recipeTreeInfos[i];

            if (info == null || info.toggle == null)
                continue;

            ItemSeries series = info.itemSeries;

            info.toggle.onValueChanged.AddListener(
                isOn => OnToggleChanged(series, isOn)
            );
        }
    }

    private void UnbindToggles()
    {
        if (recipeTreeInfos == null)
            return;

        for (int i = 0; i < recipeTreeInfos.Length; i++)
        {
            RecipeTreeInfo info = recipeTreeInfos[i];

            if (info == null || info.toggle == null)
                continue;

            info.toggle.onValueChanged.RemoveAllListeners();
        }
    }

    private void InitializePanel()
    {
        if (recipeTreeInfos == null || recipeTreeInfos.Length == 0)
            return;

        // 현재 켜져 있는 토글을 우선 사용
        for (int i = 0; i < recipeTreeInfos.Length; i++)
        {
            RecipeTreeInfo info = recipeTreeInfos[i];

            if (info == null || info.toggle == null)
                continue;

            if (info.toggle.isOn)
            {
                SelectSeries(info.itemSeries);
                return;
            }
        }

        // 켜진 토글이 없으면 첫 번째 토글 선택
        for (int i = 0; i < recipeTreeInfos.Length; i++)
        {
            RecipeTreeInfo info = recipeTreeInfos[i];

            if (info == null || info.toggle == null)
                continue;

            info.toggle.isOn = true;
            SelectSeries(info.itemSeries);
            return;
        }
    }

    private void OnToggleChanged(ItemSeries itemSeries, bool isOn)
    {
        // 꺼질 때는 처리하지 않음
        if (!isOn)
            return;

        SelectSeries(itemSeries);
    }

    public void SelectSeries(ItemSeries itemSeries)
    {
        OpenSelectedPanel(itemSeries);
        SetUI(itemSeries);
    }

    private void OpenSelectedPanel(ItemSeries selectedSeries)
    {
        if (recipeTreeInfos == null)
            return;

        for (int i = 0; i < recipeTreeInfos.Length; i++)
        {
            RecipeTreeInfo info = recipeTreeInfos[i];

            if (info == null || info.panel == null)
                continue;

            bool isSelected = info.itemSeries == selectedSeries;

            info.panel.SetActive(isSelected);
        }
    }

    public void SetUI(ItemSeries itemSeries)
    {
        if (itemCombinationManager == null)
            return;

        ItemRecipeList itemRecipeList =
            itemCombinationManager.GetRecipeListBySeries(itemSeries);

        if (itemRecipeList == null)
        {
            SetEmptyInfo();
            return;
        }

        int totalNodeCount = itemRecipeList.recipes != null
            ? itemRecipeList.recipes.Count
            : 0;

        if (totalNodeText != null)
            totalNodeText.text = totalNodeCount.ToString();

        if (depthText != null)
            depthText.text = itemRecipeList.depth.ToString();

        // 제작 기록 기능 연결 전 임시 값
        if (craftedText != null)
            craftedText.text = "0";

        if (missingText != null)
            missingText.text = totalNodeCount.ToString();
        float percent = totalNodeCount > 0
           ? 8f / totalNodeCount * 100f
           : 0f;

        if (percentText != null)
            percentText.text = $"{percent:0.#}%";

        if (percentSlider != null)
            percentSlider.value = percent / 100f;
    }

    private void SetEmptyInfo()
    {
        if (totalNodeText != null)
            totalNodeText.text = "0";

        if (depthText != null)
            depthText.text = "0";

        if (craftedText != null)
            craftedText.text = "0";

        if (missingText != null)
            missingText.text = "0";

        if (percentText != null)
            percentText.text = "0";
        if (percentSlider != null)
            percentSlider.value = 0;
    }
}