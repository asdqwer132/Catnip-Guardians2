using System;
using UnityEngine;

public class CooldownFillPreviewUI : MonoBehaviour
{
    [Header("Option")]
    public bool useSharedCooldownFill = false;

    [Header("Separate Fill")]
    public ImageFillUI currentBagCooldownFill;
    public ImageFillUI nextItemCooldownFill;

    [Header("Shared Fill")]
    public ImageFillUI sharedCooldownFill;

    public event Action OnCooldownReady;

    private bool wasBagCoolingDown;
    private bool wasNextItemCoolingDown;
    private bool initialized;

    public void Refresh(BagItemUseManager manager)
    {
        if (manager == null)
        {
            Clear();
            ResetCooldownState();
            return;
        }

        bool isBagCoolingDown = manager.IsBagCoolingDown();
        bool isNextItemCoolingDown = manager.IsNextItemUseCoolingDown();

        if (useSharedCooldownFill)
        {
            ClearSeparateFills();
            RefreshSharedFill(manager);
        }
        else
        {
            ClearSharedFill();
            RefreshSeparateFills(manager);
        }

        CheckCooldownReadyEvent(isBagCoolingDown, isNextItemCoolingDown);
    }

    private void CheckCooldownReadyEvent(bool isBagCoolingDown, bool isNextItemCoolingDown)
    {
        if (!initialized)
        {
            initialized = true;
            wasBagCoolingDown = isBagCoolingDown;
            wasNextItemCoolingDown = isNextItemCoolingDown;
            return;
        }

        bool bagCooldownEnded = wasBagCoolingDown && !isBagCoolingDown;
        bool nextItemCooldownEnded = wasNextItemCoolingDown && !isNextItemCoolingDown;

        wasBagCoolingDown = isBagCoolingDown;
        wasNextItemCoolingDown = isNextItemCoolingDown;

        if (bagCooldownEnded || nextItemCooldownEnded)
            OnCooldownReady?.Invoke();
    }

    private void RefreshSharedFill(BagItemUseManager manager)
    {
        if (sharedCooldownFill == null)
            return;

        if (manager.IsBagCoolingDown())
        {
            sharedCooldownFill.SetFill01(manager.GetBagCooldownRatio());
            return;
        }

        if (manager.IsNextItemUseCoolingDown())
        {
            sharedCooldownFill.SetFill01(manager.GetNextItemUseCooldownRatio());
            return;
        }

        sharedCooldownFill.Clear();
    }

    private void RefreshSeparateFills(BagItemUseManager manager)
    {
        if (currentBagCooldownFill != null)
            currentBagCooldownFill.SetFill01(manager.GetBagCooldownRatio());

        if (nextItemCooldownFill != null)
            nextItemCooldownFill.SetFill01(manager.GetNextItemUseCooldownRatio());
    }

    public void Clear()
    {
        ClearSharedFill();
        ClearSeparateFills();
    }

    private void ClearSharedFill()
    {
        if (sharedCooldownFill != null)
            sharedCooldownFill.Clear();
    }

    private void ClearSeparateFills()
    {
        if (currentBagCooldownFill != null)
            currentBagCooldownFill.Clear();

        if (nextItemCooldownFill != null)
            nextItemCooldownFill.Clear();
    }

    private void ResetCooldownState()
    {
        initialized = false;
        wasBagCoolingDown = false;
        wasNextItemCoolingDown = false;
    }
}