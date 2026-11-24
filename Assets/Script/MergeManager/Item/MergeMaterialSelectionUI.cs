using UnityEngine;
using UnityEngine.UI;

// Connect the authored helper buttons to exactly one material area's board.
public sealed class MergeMaterialSelectionUI : MonoBehaviour
{
    public MergegameManager board;
    public Button selectAllButton;
    public Button clearSelectionButton;
    private bool visibilityCaptured, showSelectAll, showClear;
    private void LateUpdate() => RefreshVisibility();
    private void RefreshVisibility()
    {
        if (!visibilityCaptured)
        {
            showSelectAll = selectAllButton != null && selectAllButton.gameObject.activeSelf;
            showClear = clearSelectionButton != null && clearSelectionButton.gameObject.activeSelf;
            visibilityCaptured = true;
        }
        bool visible = board != null && board.MaterialUIEnabled;
        if (selectAllButton != null && selectAllButton.gameObject.activeSelf != (visible && showSelectAll))
            selectAllButton.gameObject.SetActive(visible && showSelectAll);
        if (clearSelectionButton != null && clearSelectionButton.gameObject.activeSelf != (visible && showClear))
            clearSelectionButton.gameObject.SetActive(visible && showClear);
    }
    private void OnEnable()
    {
        if (selectAllButton != null) selectAllButton.onClick.AddListener(SelectAll);
        if (clearSelectionButton != null) clearSelectionButton.onClick.AddListener(ClearSelection);
        RefreshVisibility();
    }
    private void OnDisable()
    {
        if (selectAllButton != null) selectAllButton.onClick.RemoveListener(SelectAll);
        if (clearSelectionButton != null) clearSelectionButton.onClick.RemoveListener(ClearSelection);
    }
    public void SelectAll() => board?.SetAllMaterialsSelected(true);
    public void ClearSelection() => board?.SetAllMaterialsSelected(false);
}
