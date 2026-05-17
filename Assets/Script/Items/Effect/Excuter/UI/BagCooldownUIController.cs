using UnityEngine;

public class BagCooldownUIController : MonoBehaviour
{
    [Header("UI")]
    public BagCooldownUI[] bagCooldownUIs;

    public void Init(BagSelectManager bagSelectManager)
    {
        BuildAllBagSlotUIs(bagSelectManager);
        UpdateUI(bagSelectManager);
    }

    public void BuildAllBagSlotUIs(BagSelectManager bagSelectManager)
    {
        if (bagCooldownUIs == null)
            return;

        for (int i = 0; i < bagCooldownUIs.Length; i++)
        {
            if (bagCooldownUIs[i] == null)
                continue;

            BagItemUseManager manager = null;

            if (bagSelectManager != null)
            {
                manager = bagSelectManager.GetBagUseManager(i);
            }

            bagCooldownUIs[i].BuildSlotUIs(manager);
        }
    }

    public void UpdateUI(BagSelectManager bagSelectManager)
    {
        if (bagCooldownUIs == null)
            return;

        for (int i = 0; i < bagCooldownUIs.Length; i++)
        {
            if (bagCooldownUIs[i] == null)
                continue;

            BagItemUseManager manager = null;
            bool isSelected = false;

            if (bagSelectManager != null)
            {
                manager = bagSelectManager.GetBagUseManager(i);
                isSelected = i == bagSelectManager.CurrentBagIndex;
            }

            bagCooldownUIs[i].UpdateUI(manager, isSelected);
        }
    }
}