using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MergeCardUI : MonoBehaviour
{
    public MergeBoxSceneController boxes;
    public MergeItemSelection selection;
    public Button materialChange;
    public Image dragCooldown;
    public GameObject materialPanel;
    public RectTransform slotParent;
    public MergeMaterialSelectionUI materialHelpers;
    [Min(1)] public int maximumMaterials = 12;

    private readonly List<MergeUI> slots = new List<MergeUI>();
    private readonly List<MergeItemMaterial> boundMaterials = new List<MergeItemMaterial>();
    private readonly Dictionary<MergegameManager, HashSet<ItemData>> selections = new Dictionary<MergegameManager, HashSet<ItemData>>();
    private MergegameManager boundBoard;
    private bool slotsCached;

    public int MaterialCapacity
    {
        get
        {
            CacheSlots();
            return Mathf.Min(maximumMaterials, slots.Count);
        }
    }

    private void CacheSlots()
    {
        if (slotsCached || slotParent == null) return;
        slotsCached = true;
        foreach (Transform child in slotParent)
            if (child.TryGetComponent<MergeUI>(out var slot)) slots.Add(slot);
    }

    private void OnEnable()
    {
        if (materialPanel != null) materialPanel.SetActive(true);
        if (materialChange != null) materialChange.onClick.AddListener(AdvanceMaterial);
    }

    private void OnDisable()
    {
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

    private void AdvanceMaterial() => boxes?.SelectedBoard?.AdvanceSelectedMaterial();

    public void Bind(MergegameManager board)
    {
        int capacity = MaterialCapacity;
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
        if (board == null)
        {
            HideUnusedSlots();
            return;
        }
        if (!selections.TryGetValue(board, out var remembered))
            selections.Add(board, remembered = new HashSet<ItemData>());
        foreach (var material in board.mergeMaterials)
        {
            var item = material?.mergeItem?.item;
            if (item == null || boundMaterials.Count >= capacity) continue;
            int index = boundMaterials.Count;
            var slot = slots[index];
            material.mergeUI = slot;
            boundMaterials.Add(material);
            if (slot.itemIcon != null) slot.itemIcon.sprite = item.icon;
            if (slot.toggle != null) slot.toggle.SetIsOnWithoutNotify(material.count > 0 && remembered.Contains(item));
            slot.gameObject.SetActive(true);
        }
        HideUnusedSlots();
        board.RefreshUI();
    }

    private void HideUnusedSlots()
    {
        for (int i = boundMaterials.Count; i < slots.Count; i++)
            slots[i].gameObject.SetActive(false);
    }
}
