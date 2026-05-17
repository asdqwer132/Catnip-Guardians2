using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeBuffStatusSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Image fillImage;
    public TextMeshProUGUI numberText;

    private RuntimeBuffStatusUI owner;
    private RuntimeBuffUIInfo info;
    private int startNumber;

    private void Awake()
    {
        owner = GetComponentInParent<RuntimeBuffStatusUI>();
    }

    private void Update()
    {
        Refresh();
    }

    public void Bind(RuntimeBuffUIInfo newInfo)
    {
        info = newInfo;

        if (info == null)
        {
            RemoveSelf();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = info.icon;
            iconImage.gameObject.SetActive(info.icon != null);
        }

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = 2;
            fillImage.fillClockwise = false;
            fillImage.fillAmount = 1f;

            fillImage.gameObject.SetActive(info.icon != null);
        }

        if (info.startNumber > 0)
        {
            startNumber = info.startNumber;
        }
        else
        {
            startNumber = GetDisplayNumber();
        }

        if (startNumber <= 0)
            startNumber = 1;

        Refresh();
    }

    public bool IsSameBuff(RuntimeBuffUIInfo other)
    {
        if (info == null || other == null)
            return false;

        if (info.source != other.source)
            return false;

        if (info.durationType != other.durationType)
            return false;

        if (info.scope != other.scope)
            return false;

        if (info.scope == RuntimeBuffUIScope.AllBags)
            return true;

        return info.currentBag == other.currentBag;
    }

    private void Refresh()
    {
        if (info == null)
        {
            RemoveSelf();
            return;
        }

        int currentNumber = GetDisplayNumber();

        if (currentNumber <= 0)
        {
            RemoveSelf();
            return;
        }

        if (numberText != null)
        {
            numberText.text = currentNumber.ToString();
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = GetFillAmount(currentNumber);
        }
    }

    private int GetDisplayNumber()
    {
        if (info == null)
            return 0;

        if (info.bagItems == null)
            return startNumber;

        int totalUseCount = 0;
        int maxTime = 0;

        for (int i = 0; i < info.bagItems.Count; i++)
        {
            InventoryItem item = info.bagItems[i];

            if (item == null)
                continue;

            int itemNumber = item.GetRuntimeBuffDisplayNumber(
                info.source,
                info.durationType,
                info.currentCycleId
            );

            if (itemNumber <= 0)
                continue;

            if (info.durationType == RuntimeBuffDurationType.NextItemUse)
            {
                totalUseCount += itemNumber;
            }
            else if (info.durationType == RuntimeBuffDurationType.Seconds)
            {
                maxTime = Mathf.Max(maxTime, itemNumber);
            }
        }

        if (info.durationType == RuntimeBuffDurationType.NextItemUse)
        {
            if (totalUseCount > 0)
                return totalUseCount;

            return startNumber;
        }

        if (info.durationType == RuntimeBuffDurationType.Seconds)
        {
            if (maxTime > 0)
                return maxTime;

            return startNumber;
        }

        return startNumber;
    }

    private float GetFillAmount(int currentNumber)
    {
        if (startNumber <= 0)
            return 0f;

        return Mathf.Clamp01((float)currentNumber / startNumber);
    }

    private void RemoveSelf()
    {
        if (owner != null)
        {
            owner.RemoveSlot(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}