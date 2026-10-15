using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public sealed class MergeUpgradeSceneController : MonoBehaviour
{
    public MergeGameCatalog catalog;
    public GameObject skillPanel, shopPanel, subPlantPanel;
    public GameObject[] removedPanels, removedNavigation;
    public Behaviour[] removedControllers;
    public RectTransform navigationParent;
    public UpgradeManager upgradeManager;
    public SubPlantManager subPlantManager;
    private RectTransform customContent, skillRows, dictionaryRows;
    private GameObject containerTree, dictionary;
    private TextMeshProUGUI statsText;
    private readonly List<Button> purchaseButtons = new List<Button>();
    private readonly List<SkillNodeData> displayedNodes = new List<SkillNodeData>();
    private InventoryManager inventory;
    private SkillTreeManager skills;
    private GameInputManager input;
    private int selectedSeries;
    private bool enemyDictionary;
    private string search = "";
    private int dictionaryPage;
    private TextMeshProUGUI pageStatus;
    private Button previousPage, nextPage;
    private void Awake()
    {
        MergeGameSession.Ensure();
        foreach (var go in removedPanels ?? new GameObject[0]) if (go != null) go.SetActive(false);
        foreach (var go in removedNavigation ?? new GameObject[0]) if (go != null) go.SetActive(false);
        foreach (var component in removedControllers ?? new Behaviour[0]) if (component != null) component.enabled = false;
    }
    private void Start()
    {
        inventory = InventoryManager.instance;
        if (inventory != null) { MergeGameSession.Instance.RestoreInventory(inventory); inventory.onInventoryChanged += SaveStock; SaveStock(); }
        if (subPlantManager != null)
        {
            var session = MergeGameSession.Instance;
            if (session.hasSubPlantState)
            {
                subPlantManager.currentMaterials.Clear();
                foreach (var item in session.subPlantMaterials) subPlantManager.currentMaterials.Add(new InventoryItem(item.itemData, item.amount));
                subPlantManager.Combine(); subPlantManager.onMaterialChanged?.Invoke();
            }
            subPlantManager.onMaterialChanged += SaveSubPlant;
        }
        skills = SkillTreeManager.Instance;
        if (skills != null) skills.OnSkillTreeChanged += RefreshSkills;
        BuildUI(); ShowTab(0); BindInput();
    }
    private void BindInput()
    {
        if (input == GameInputManager.instance || GameInputManager.instance == null) return;
        input = GameInputManager.instance;
        input.OnSkillTreePressed += ShowSkills; input.OnShopPressed += ShowShop; input.OnInventoryPressed += ShowDictionary;
    }
    private void ShowSkills() => ShowTab(0);
    private void ShowShop() => ShowTab(2);
    private void ShowDictionary() => ShowTab(3);
    private void SaveStock() { if (inventory != null) MergeGameSession.Ensure().SaveInventory(inventory); }
    private void SaveSubPlant()
    { if (subPlantManager != null) MergeGameSession.Ensure().SaveSubPlant(subPlantManager.currentMaterials, subPlantManager.resultItem); }
    private void BuildUI()
    {
        if (catalog == null || navigationParent == null) return;
        var nav = MergeGameUI.Panel("Upgrade Navigation", navigationParent, Vector2.zero, Vector2.one, new Color(0.06f, 0.09f, 0.13f));
        nav.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 8f;
        string[] names = { "스킬트리", "보조식물", "상자 구매", "사전" };
        for (int i = 0; i < names.Length; i++) { int index = i; MergeGameUI.Button(nav.transform, names[i], catalog.UIFont, () => ShowTab(index)); }
        MergeGameUI.Button(nav.transform, "전투 시작", catalog.UIFont, () => { if (upgradeManager != null) upgradeManager.GoGame(); });
        Transform parent = skillPanel != null ? skillPanel.transform.parent : navigationParent.parent;
        customContent = MergeGameUI.Rect("Merge Upgrade Content", parent, Vector2.zero, Vector2.one);
        if (skillPanel != null)
        {
            var rect = skillPanel.GetComponent<RectTransform>();
            customContent.anchorMin = rect.anchorMin; customContent.anchorMax = rect.anchorMax;
            customContent.pivot = rect.pivot; customContent.sizeDelta = rect.sizeDelta; customContent.anchoredPosition = rect.anchoredPosition;
        }
        BuildSkills(); BuildDictionary();
    }
    private void BuildSkills()
    {
        var root = MergeGameUI.Panel("Container Skill Tree", customContent, Vector2.zero, Vector2.one, new Color(0.04f, 0.065f, 0.1f));
        containerTree = root.gameObject;
        var tabs = MergeGameUI.Rect("Series Tabs", root.transform, new Vector2(0, 0.9f), Vector2.one);
        tabs.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 4f;
        for (int i = 0; catalog.containers != null && i < catalog.containers.Length; i++)
        {
            int index = i; var definition = catalog.containers[i]; if (definition == null) continue;
            MergeGameUI.Button(tabs, definition.displayName, catalog.UIFont, () => { selectedSeries = index; RebuildSkills(); });
        }
        statsText = MergeGameUI.Label(MergeGameUI.Rect("Current Stats", root.transform, new Vector2(0, 0.76f), new Vector2(1, 0.9f)), "", catalog.UIFont, 19);
        skillRows = MergeGameUI.Scroll(MergeGameUI.Rect("Skill Branches", root.transform, new Vector2(0, 0.08f), new Vector2(1, 0.76f)));
        var footer = MergeGameUI.Rect("Other Skills", root.transform, Vector2.zero, new Vector2(1, 0.075f));
        MergeGameUI.Button(footer, "기존 특수 스킬 보기", catalog.UIFont, () => { customContent.gameObject.SetActive(false); ShowPanel(skillPanel, true); });
        RebuildSkills();
    }
    private MergeContainerDefinition SelectedDefinition() => catalog.containers != null && selectedSeries < catalog.containers.Length ? catalog.containers[selectedSeries] : null;
    private void RebuildSkills()
    {
        if (skillRows == null) return;
        foreach (Transform child in skillRows) Destroy(child.gameObject);
        displayedNodes.Clear(); purchaseButtons.Clear(); var definition = SelectedDefinition();
        foreach (var node in catalog.containerSkills ?? new SkillNodeData[0])
        {
            if (node == null || node.rewards == null) continue;
            bool applies = false;
            foreach (var reward in node.rewards)
                if (reward is MergeContainerUpgradeReward container && (container.container == null || container.container == definition)) applies = true;
            if (!applies) continue;
            var row = MergeGameUI.Row(skillRows, 74f);
            var label = MergeGameUI.Rect("Skill Name", row, Vector2.zero, new Vector2(0.7f, 1f));
            string description = node.GetDescription(LanguageManager.instance != null ? LanguageManager.instance.selectedLan : default(language));
            MergeGameUI.Label(label, MergeGameUI.Name(node) + "\n" + description, catalog.UIFont, 18);
            var purchase = MergeGameUI.Rect("Purchase", row, new Vector2(0.71f, 0), Vector2.one);
            displayedNodes.Add(node); purchaseButtons.Add(MergeGameUI.Button(purchase, "", catalog.UIFont, () =>
            { if (SkillTreeManager.Instance != null) SkillTreeManager.Instance.UnlockSkill(node); RefreshSkills(); }));
        }
        RefreshSkills();
    }
    private void RefreshSkills()
    {
        var definition = SelectedDefinition();
        if (definition != null && statsText != null)
        {
            var stat = MergeGameSession.Ensure().upgrades.Resolve(definition);
            statsText.text = $"{definition.displayName} · 크기 {stat.width:0.0} × {stat.height:0.0} · {stat.injectionInterval:0.00}초마다 투입 · 주입구 {stat.inletCount}개\n" +
                $"드래그 반경 {stat.dragRadius:0.00} · 클릭 대기 {stat.clickCooldown:0.00}초 · 드래그 대기 {stat.dragCooldown:0.00}초";
        }
        for (int i = 0; i < purchaseButtons.Count; i++)
        {
            var node = displayedNodes[i]; bool owned = SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked(node.dataId);
            purchaseButtons[i].interactable = !owned && SkillTreeManager.Instance != null && SkillTreeManager.Instance.CanUnlock(node);
            string price = ""; foreach (var cost in node.costs ?? new Cost[0]) if (cost != null) price += $"{cost.currencyType} {cost.amount} ";
            purchaseButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = owned ? "해금 완료" : price.Trim();
        }
    }
    private void BuildDictionary()
    {
        var root = MergeGameUI.Panel("Encyclopedia", customContent, Vector2.zero, Vector2.one, new Color(0.04f, 0.065f, 0.1f));
        dictionary = root.gameObject;
        var tabs = MergeGameUI.Rect("Category", root.transform, new Vector2(0, 0.91f), Vector2.one);
        tabs.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 8f;
        MergeGameUI.Button(tabs, "아이템", catalog.UIFont, () => { enemyDictionary = false; dictionaryPage = 0; RebuildDictionary(); });
        MergeGameUI.Button(tabs, "적", catalog.UIFont, () => { enemyDictionary = true; dictionaryPage = 0; RebuildDictionary(); });
        var searchRect = MergeGameUI.Rect("Search", root.transform, new Vector2(0.02f, 0.83f), new Vector2(0.98f, 0.90f));
        searchRect.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.27f);
        var inputField = searchRect.gameObject.AddComponent<TMP_InputField>();
        var area = MergeGameUI.Rect("Text Area", searchRect, Vector2.zero, Vector2.one); area.gameObject.AddComponent<RectMask2D>();
        inputField.textViewport = area;
        inputField.textComponent = MergeGameUI.Label(area, "", catalog.UIFont, 20);
        var placeholder = MergeGameUI.Label(area, "이름 또는 시리즈 검색", catalog.UIFont, 20); placeholder.color = Color.gray; inputField.placeholder = placeholder;
        inputField.onValueChanged.AddListener(value => { search = value; dictionaryPage = 0; RebuildDictionary(); });
        dictionaryRows = MergeGameUI.Scroll(MergeGameUI.Rect("Entries", root.transform, new Vector2(0, 0.075f), new Vector2(1, 0.82f)));
        var paging = MergeGameUI.Rect("Pages", root.transform, Vector2.zero, new Vector2(1, 0.07f));
        paging.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 8f;
        previousPage = MergeGameUI.Button(paging, "이전", catalog.UIFont, () => { dictionaryPage = Mathf.Max(0, dictionaryPage - 1); RebuildDictionary(); });
        pageStatus = MergeGameUI.Label(paging, "", catalog.UIFont, 18);
        nextPage = MergeGameUI.Button(paging, "다음", catalog.UIFont, () => { dictionaryPage++; RebuildDictionary(); });
        RebuildDictionary();
    }
    private bool Matches(string value) => string.IsNullOrWhiteSpace(search) || value.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
    private void RebuildDictionary()
    {
        if (dictionaryRows == null) return;
        foreach (Transform child in dictionaryRows) Destroy(child.gameObject);
        var seen = new HashSet<Object>();
        int matches = 0, first = dictionaryPage * 30;
        if (enemyDictionary)
        {
            foreach (var enemy in catalog.encyclopediaEnemies ?? new EnemyStatData[0])
            {
                if (enemy == null || !seen.Add(enemy)) continue;
                string name = string.IsNullOrEmpty(enemy.enemyClass) ? enemy.name : enemy.enemyClass + " · " + enemy.name;
                if (!Matches(name)) continue;
                if (matches++ < first || matches > first + 30) continue;
                var row = MergeGameUI.Row(dictionaryRows, 100);
                MergeGameUI.Label(row, $"{name}\n체력 {enemy.maxHp:0.#} · 이동 {enemy.speed:0.#} · 피해 {enemy.damage:0.#}\n사거리 {enemy.minAttackRange:0.#}~{enemy.maxAttackRange:0.#} · 공격 간격 {enemy.minAttackCooldown:0.#}~{enemy.maxAttackCooldown:0.#}초 · 성장치 {enemy.minGrowEx:0.#}~{enemy.maxGrowEx:0.#}", catalog.UIFont, 18);
            }
        }
        else
        {
            foreach (var item in catalog.encyclopediaItems ?? new ItemData[0])
            {
                if (item == null || !seen.Add(item)) continue;
                string name = MergeGameUI.Name(item); if (!Matches(name + " " + item.series)) continue;
                if (matches++ < first || matches > first + 30) continue;
                var row = MergeGameUI.Row(dictionaryRows, 150);
                var icon = MergeGameUI.Panel("Icon", row, new Vector2(0, 0.1f), new Vector2(0.075f, 0.9f), Color.white);
                icon.sprite = item.icon; icon.preserveAspect = true; icon.enabled = item.icon != null;
                string description = item.GetDescription(LanguageManager.instance != null ? LanguageManager.instance.selectedLan : default(language));
                var label = MergeGameUI.Rect("Details", row, new Vector2(0.085f, 0), Vector2.one);
                string merge = "";
                foreach (var container in catalog.containers ?? new MergeContainerDefinition[0])
                {
                    var entry = container != null ? container.Find(item) : null;
                    if (entry != null) { merge = entry.mergeResult != null ? "\n같은 아이템 2개 → " + MergeGameUI.Name(entry.mergeResult) : "\n최종 합성 단계"; break; }
                }
                MergeGameUI.Label(label, $"{name} · {item.series} · {item.grade}\n{description}\n분류 {item.category} · 보유 {(inventory != null ? inventory.GetItemAmount(item) : 0)}개" + merge, catalog.UIFont, 18);
            }
        }
        int pages = Mathf.Max(1, (matches + 29) / 30);
        if (dictionaryPage >= pages) { dictionaryPage = pages - 1; RebuildDictionary(); return; }
        if (pageStatus != null) pageStatus.text = $"{dictionaryPage + 1} / {pages} · {matches}개";
        if (previousPage != null) previousPage.interactable = dictionaryPage > 0;
        if (nextPage != null) nextPage.interactable = dictionaryPage + 1 < pages;
    }
    public void ShowTab(int index)
    {
        ShowPanel(skillPanel, false); ShowPanel(shopPanel, index == 2); ShowPanel(subPlantPanel, index == 1);
        if (customContent != null) customContent.gameObject.SetActive(index == 0 || index == 3);
        if (containerTree != null) containerTree.SetActive(index == 0);
        if (dictionary != null) dictionary.SetActive(index == 3);
        if (index == 0) RefreshSkills(); if (index == 3) RebuildDictionary();
    }
    private static void ShowPanel(GameObject panel, bool active)
    {
        if (panel == null) return;
        panel.SetActive(active); var group = panel.GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = active ? 1f : 0f; group.interactable = group.blocksRaycasts = active; }
    }
    private float refreshTimer;
    private void Update() { BindInput(); refreshTimer += Time.unscaledDeltaTime; if (refreshTimer > 0.5f) { refreshTimer = 0f; RefreshSkills(); } }
    private void OnDestroy()
    {
        if (inventory != null) { SaveStock(); inventory.onInventoryChanged -= SaveStock; }
        if (subPlantManager != null) { SaveSubPlant(); subPlantManager.onMaterialChanged -= SaveSubPlant; }
        if (skills != null) skills.OnSkillTreeChanged -= RefreshSkills;
        if (input != null) { input.OnSkillTreePressed -= ShowSkills; input.OnShopPressed -= ShowShop; input.OnInventoryPressed -= ShowDictionary; }
    }
}
