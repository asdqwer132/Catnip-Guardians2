using UnityEngine;

public class BagCooldownPreviewSlotUI : BagUIBase
{
    [Header("Cooldown Preview")]
    public CooldownFillPreviewUI cooldownFillPreviewUI;

    private BagItemUseManager useManager;

    protected override void RefreshUI(EquipmentBag bag)
    {
        useManager = null;

        if (bag == null)
        {
            ClearUI();
            return;
        }

        if (BagCooldownManager.instance != null)
            useManager = BagCooldownManager.instance.GetManagerByBag(bag);

        RefreshCooldown();
    }

    private void Update()
    {
        RefreshCooldown();
    }

    private void RefreshCooldown()
    {
        if (cooldownFillPreviewUI == null)
            return;

        if (useManager == null)
        {
            cooldownFillPreviewUI.Clear();
            return;
        }

        cooldownFillPreviewUI.Refresh(useManager);
    }

    protected override void ClearUI()
    {
        useManager = null;

        if (bagIcon != null)
        {
            bagIcon.enabled = false;
            bagIcon.sprite = null;
        }

        if (cooldownFillPreviewUI != null)
            cooldownFillPreviewUI.Clear();
    }
}
