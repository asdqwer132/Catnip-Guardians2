using UnityEngine;

public class SubPlantUIManager : MonoBehaviour
{
    public SubPlantManager subplantmanager;
    public SubPlantMaterialSlotUI[] slots;
    public InventorySlotUI resultSlot;
    public ToggleTooltipTrigger trigger;
    public CraftSuccessEffectUI successEffect;

    public bool dynamicCombine = false;

    void Start()
    {
        RefreshUI();
        resultSlot.ClearSlot();

        if (subplantmanager != null)
        {
            subplantmanager.onMaterialChanged += RefreshUI;
        }
    }

    void OnDestroy()
    {
        if (subplantmanager != null)
        {
            subplantmanager.onMaterialChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {

        var materials = subplantmanager.currentMaterials;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < materials.Count)
            {
                slots[i].SetSlot(materials[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
        subplantmanager.Combine();
        Combine(dynamicCombine);
    }
    public void Combine(bool dynamic)
    {
        ItemData resultItem = subplantmanager.resultItem;
        if (dynamic)
        {
            //제작 성공
            resultSlot.SetSlot(new InventoryItem(resultItem, 1));

            if (!subplantmanager.isEmptyResult())
                successEffect.Play(resultItem.icon);
        }
        else
            resultSlot.ClearSlot();

        if (subplantmanager.isEmptyResult())
            trigger.HideTooltip();
        else
        {

            trigger.ShowTooltip();
        }
    }
}