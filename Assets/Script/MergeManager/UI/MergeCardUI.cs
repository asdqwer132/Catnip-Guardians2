using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MergeCardUI : MonoBehaviour
{
    public MergeBoxSceneController boxes;
    public MergeItemSelection selection;
    public Button materialSelector;
    public Button materialChange;
    public Image dragCooldown;
    public GameObject materialPanel;
    public RectTransform slotParent;
    public MergeUI slotPrefab;
    public MergeMaterialSelectionUI materialHelpers;
    [Min(1)] public int maximumMaterials = 12;

    private readonly List<MergeUI> slots = new List<MergeUI>();
    private readonly List<MergeItemMaterial> boundMaterials = new List<MergeItemMaterial>();
    private readonly Dictionary<MergegameManager, HashSet<ItemData>> selections = new Dictionary<MergegameManager, HashSet<ItemData>>();
    private MergegameManager boundBoard;

    private void OnEnable()
    {
        if (materialPanel != null) materialPanel.SetActive(false);
        if (materialSelector != null) materialSelector.onClick.AddListener(TogglePanel);
        if (materialChange != null) materialChange.onClick.AddListener(AdvanceMaterial);
    }

    private void OnDisable()
    {
        if (materialSelector != null) materialSelector.onClick.RemoveListener(TogglePanel);
        if (materialChange != null) materialChange.onClick.RemoveListener(AdvanceMaterial);
    }

    private void LateUpdate()
    {
        if (dragCooldown != null)
        {
            float progress = selection != null ? selection.CooldownProgress : 0f;
            dragCooldown.fillAmount = progress;
            dragCooldown.enabled = progress > 0f;
        }
    }

    private void TogglePanel()
    {
        if (materialPanel != null) materialPanel.SetActive(!materialPanel.activeSelf);
    }

    private void AdvanceMaterial() => boxes?.SelectedBoard?.AdvanceSelectedMaterial();

    public void Bind(MergegameManager board)
    {
        if (slotPrefab == null || slotParent == null) return;
        if (boundBoard != null)
        {
            var selected = selections[boundBoard];
            selected.Clear();
            foreach (var material in boundMaterials)
            {
                if (material.mergeUI != null && material.mergeUI.toggle != null && material.mergeUI.toggle.isOn)
                    selected.Add(material.mergeItem.item);
                material.mergeUI = null;
            }
            boundBoard.RefreshUI();
        }
        boundMaterials.Clear();
        boundBoard = board;
        if (materialHelpers != null) materialHelpers.board = board;
        if (board == null) return;
        if (!selections.TryGetValue(board, out var remembered))
            selections.Add(board, remembered = new HashSet<ItemData>());
        foreach (var material in board.mergeMaterials)
        {
            var item = material?.mergeItem?.item;
            if (item == null || boundMaterials.Count >= maximumMaterials) continue;
            int index = boundMaterials.Count;
            if (slots.Count <= index) slots.Add(Instantiate(slotPrefab, slotParent));
            var slot = slots[index];
            material.mergeUI = slot;
            boundMaterials.Add(material);
            if (slot.itemIcon != null) slot.itemIcon.sprite = item.icon;
            if (slot.toggle != null) slot.toggle.SetIsOnWithoutNotify(material.count > 0 && remembered.Contains(item));
            slot.gameObject.SetActive(true);
        }
        for (int i = boundMaterials.Count; i < slots.Count; i++) slots[i].gameObject.SetActive(false);
        board.RefreshUI();
    }
}
