using System.Text;
using UnityEngine;

public class BagTooltipProvider : MonoBehaviour, ITooltipContentProvider
{
    [Header("Target")]
    [SerializeField] private EquipmentBag equipmentBag;
    [SerializeField] private BagData bagData;

    [Header("Anchor")]
    [SerializeField] private RectTransform anchorRect;

    [Header("Text")]
    [SerializeField] private string bagTypeText = "Bag";
    [SerializeField] private string emptyDescriptionText = "NoInfo.";

    [Header("Option")]
    [SerializeField] private bool showSlotInfo = true;
    [SerializeField] private bool showWeightInfo = true;
    [SerializeField] private bool showEquippedInfo = true;
    [SerializeField] private bool showLockInfo = true;

    private void Awake()
    {
        if (equipmentBag == null)
            equipmentBag = GetComponent<EquipmentBag>();

        if (anchorRect == null)
            anchorRect = transform as RectTransform;

        if (bagData == null && equipmentBag != null)
            bagData = equipmentBag.bagData;
    }

    public bool TryGetTooltipData(out TooltipData data)
    {
        data = null;

        BagData targetBagData = GetTargetBagData();

        if (targetBagData == null)
            return false;

        data = new TooltipData
        {
            icon = targetBagData.icon,
            title = GetBagName(targetBagData),
            subTitle = bagTypeText,
            amountText = "",
            description = BuildDescription(targetBagData)
        };

        return true;
    }

    public RectTransform GetTooltipAnchor()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;

        return anchorRect;
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

        if (LanguageManager.instance != null)
        {
            string dataName = targetBagData.GetDataName();

            if (!string.IsNullOrEmpty(dataName))
                return dataName;
        }

        return targetBagData.name;
    }

    private string GetBagDescription(BagData targetBagData)
    {
        if (targetBagData == null)
            return "";

        if (LanguageManager.instance != null)
        {
            string description = targetBagData.GetDescription();

            if (!string.IsNullOrEmpty(description))
                return description;
        }

        return emptyDescriptionText;
    }

    private string BuildDescription(BagData targetBagData)
    {
        StringBuilder sb = new StringBuilder();

        string baseDescription = GetBagDescription(targetBagData);

        if (!string.IsNullOrEmpty(baseDescription))
            sb.AppendLine(baseDescription);

        AppendRuntimeInfo(sb, targetBagData);
        AppendLockInfo(sb);

        return sb.ToString().TrimEnd();
    }

    private void AppendRuntimeInfo(StringBuilder sb, BagData targetBagData)
    {
        AppendSectionGap(sb);
        sb.AppendLine("[BagInfo]");

        if (showSlotInfo)
        {
            if (equipmentBag != null)
            {
                sb.AppendLine($"- Slot: {equipmentBag.currentSlotCount} / {equipmentBag.slotCount}");
                sb.AppendLine($"- DefaultSlot: {equipmentBag.openSlotCount}");
            }
            else
            {
                sb.AppendLine($"- Slot: {targetBagData.slotCount}");
            }
        }

        if (showWeightInfo)
        {
            if (equipmentBag != null)
            {
                float currentWeight = equipmentBag.GetCurrentWeight();
                float maxWeight = equipmentBag.GetMaxWeight();

                sb.AppendLine($"- Weight: {currentWeight:0.#} / {maxWeight:0.#}");
            }
            else
            {
                sb.AppendLine($"- MaxWeight: {targetBagData.maxWeight}");
            }
        }

        if (showEquippedInfo && equipmentBag != null)
        {
            int currentEquippedCount = equipmentBag.GetCurrentEquippedCount();
            sb.AppendLine($"- Equiped: {currentEquippedCount} / {equipmentBag.currentSlotCount}");
        }
    }

    private void AppendLockInfo(StringBuilder sb)
    {
        if (!showLockInfo)
            return;

        if (equipmentBag == null || equipmentBag.locks == null || equipmentBag.locks.Count == 0)
            return;

        int lockedCount = 0;
        int unlockedCount = 0;

        for (int i = 0; i < equipmentBag.locks.Count; i++)
        {
            LockInfo lockInfo = equipmentBag.locks[i];

            if (lockInfo == null)
                continue;

            if (lockInfo.locked)
                lockedCount++;
            else
                unlockedCount++;
        }

        AppendSectionGap(sb);
        sb.AppendLine($"[Locked] : {unlockedCount} / {lockedCount}");
    }

    private void AppendSectionGap(StringBuilder sb)
    {
        if (sb.Length > 0)
            sb.AppendLine();
    }
}