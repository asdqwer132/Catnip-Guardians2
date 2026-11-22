using UnityEngine;
using UnityEngine.UI;

public sealed class ChestEquipmentSelectorUI : MonoBehaviour
{
    [SerializeField] private BagEquipmentSlotUI chestSlot;
    [SerializeField] private Button nextButton;
    private GameSession session;

    private void OnEnable()
    {
        session = GameSession.GetOrCreate();
        session.GameDataChanged += Refresh;
        nextButton.onClick.AddListener(SelectNext);
        Refresh();
    }

    private void OnDisable()
    {
        if (session != null) session.GameDataChanged -= Refresh;
        nextButton.onClick.RemoveListener(SelectNext);
    }

    private void Refresh()
    {
        session.TryReadGeneralData(out var data);
        chestSlot.Show(EquipmentSave.ResolveChest(EquipmentSave.Current, data?.unlockedMergeBoxes), false);
        if (chestSlot.slotToggle != null) chestSlot.slotToggle.interactable = false;
    }

    private void SelectNext()
    {
        session.TryReadGeneralData(out var data);
        var equipment = EquipmentSave.Current.Copy();
        int current = EquipmentSave.ResolveChest(equipment, data?.unlockedMergeBoxes)?.boxIndex ?? 1;
        for (int offset = 1; offset < 8; offset++)
        {
            int number = (current - 1 + offset) % 8 + 1;
            if (!EquipmentSave.IsChestUnlocked(number, data?.unlockedMergeBoxes) || EquipmentSave.FindChest(number) == null)
                continue;

            equipment.equippedChests.Clear();
            equipment.equippedChests.Add(number);
            if (EquipmentSave.Write(equipment))
            {
                session.UpdateEquipment(equipment);
                Refresh();
            }
            return;
        }
    }
}
