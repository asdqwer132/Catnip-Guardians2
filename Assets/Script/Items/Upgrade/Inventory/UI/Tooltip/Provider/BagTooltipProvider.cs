using System.Text;
using UnityEngine;

public class BagTooltipProvider : TooltipProvider
{
    [Header("Target")]
    [SerializeField] private EquipmentBag equipmentBag;
    private BagData bagData;

    [Header("Text")]
    [SerializeField] private string bagTypeText = "Bag";
    [SerializeField] private string emptyDescriptionText = "NoInfo.";


    protected override void Awake()
    {
        base.Awake();

        if (equipmentBag == null)
            equipmentBag = GetComponent<EquipmentBag>();

        if (bagData == null && equipmentBag != null)
            bagData = equipmentBag.bagData;
    }

    public override bool TryGetTooltipData(out TooltipData data)
    {
        data = null;

        BagData targetBagData = GetTargetBagData();

        if (targetBagData == null)
            return false;

        data = new BagTooltipData
        {
            icon = targetBagData.icon,
            title = GetBagName(targetBagData),
            subTitle = bagTypeText,
            amountText = "",
            description = GetBagDescription(targetBagData),
            weight = GetWeight(targetBagData),
            slots = GetSlots(targetBagData)
        };

        return true;
    }
    private string GetSlots(BagData targetBagData)
    {
        return equipmentBag.currentSlotCount + "/" + equipmentBag.maxSlotCount;
    }
    private string GetWeight(BagData targetBagData)
    {
        return targetBagData.maxWeight + "/" + targetBagData.maxWeight;
    }
    private BagData GetTargetBagData()
    {
        if (equipmentBag != null && equipmentBag.bagData != null)
            return equipmentBag.bagData;

        return bagData;
    }

    private string GetBagName(BagData targetBagData)
    {
        if (targetBagData == null)
            return "";

        string dataName = targetBagData.GetDataName();

        if (!string.IsNullOrEmpty(dataName))
            return dataName;

        return targetBagData.name;
    }

    private string GetBagDescription(BagData targetBagData)
    {
        if (targetBagData == null)
            return "";

        string description = targetBagData.GetDescription();

        if (!string.IsNullOrEmpty(description))
            return description;

        return emptyDescriptionText;
    }
}