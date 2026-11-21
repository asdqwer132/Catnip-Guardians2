using UnityEngine;

public sealed class PlayerEquipmentSlotUI : BagEquipmentSlotUI
{
    [SerializeField] private SlotUI normalPlantPreview;
    [SerializeField] private SlotUI bossPlantPreview;

    public override void Show(DefaultData data, bool selected, bool isLocked = false)
    {
        base.Show(data, selected, isLocked);
        var player = data as PlayerData;
        normalPlantPreview?.SetData(player?.normalPlant);
        bossPlantPreview?.SetData(player?.bossPlant);
    }
}
