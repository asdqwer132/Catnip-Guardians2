using UnityEngine;

public class BagCooldownUIController : MonoBehaviour
{
    [System.Serializable]
    public class BagCooldownUIBinding
    {
        [Header("Target")]
        public int bagIndex;

        [Header("UI")]
        public BagCooldownUI cooldownUI;
    }

    [Header("UI Bindings")]
    public BagCooldownUIBinding[] bindings;

    private BagSelectManager bagSelectManager;

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Init(BagSelectManager targetBagSelectManager)
    {
        Unsubscribe();

        bagSelectManager = targetBagSelectManager;

        Subscribe();
        BuildAllBagSlotUIs();
        RefreshSelection();
    }

    public void RefreshSelection()
    {
        if (bindings == null)
            return;

        for (int i = 0; i < bindings.Length; i++)
        {
            BagCooldownUIBinding binding = bindings[i];

            if (binding == null || binding.cooldownUI == null)
                continue;

            BagItemUseManager manager = GetManager(binding.bagIndex);
            bool isSelected = bagSelectManager != null && binding.bagIndex == bagSelectManager.CurrentBagIndex;

            binding.cooldownUI.RefreshUI(manager, isSelected);
        }
    }

    public void BuildAllBagSlotUIs()
    {
        if (bindings == null)
            return;

        for (int i = 0; i < bindings.Length; i++)
        {
            BagCooldownUIBinding binding = bindings[i];

            if (binding == null || binding.cooldownUI == null)
                continue;

            BagItemUseManager manager = GetManager(binding.bagIndex);
            binding.cooldownUI.BuildSlotUIs(manager);
        }
    }

    private void Subscribe()
    {
        if (bagSelectManager != null)
            bagSelectManager.OnBagSelected += HandleBagSelected;
    }

    private void Unsubscribe()
    {
        if (bagSelectManager != null)
            bagSelectManager.OnBagSelected -= HandleBagSelected;
    }

    private void HandleBagSelected(int bagIndex)
    {
        RefreshSelection();
    }

    private BagItemUseManager GetManager(int bagIndex)
    {
        if (bagSelectManager == null)
            return null;

        if (bagIndex < 0)
            return null;

        return bagSelectManager.GetBagUseManager(bagIndex);
    }
}