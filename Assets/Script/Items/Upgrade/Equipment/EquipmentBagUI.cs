using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentBagUI : MonoBehaviour
{
    [Header("Slots")]
    public EquipmentSlotUI[] slots;

    [Header("Bag Icon")]
    public Image bagIcon;

    [Header("Weight UI")]
    public Slider weightSlider;
    public TextMeshProUGUI weightText;

    [Header("Drag Icon")]
    public Image dragIconImage;

    private EquipmentBag bag;

    public void Init(BagData bagData)
    {
        if (bagData != null && bagIcon != null)
            bagIcon.sprite = bagData.icon;

        InitSlots();

        if (dragIconImage != null)
            dragIconImage.gameObject.SetActive(false);
    }

    private void InitSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Init(this, dragIconImage, i);
        }
    }

    public void Refresh(EquipmentBag targetBag)
    {
        bag = targetBag;

        if (bag == null)
        {
            ClearUI();
            return;
        }

        if (bag.bagData != null && bagIcon != null)
            bagIcon.sprite = bag.bagData.icon;

        InitSlots();

        int currentCount = bag.currentSlotCount;
        for (int i = 0; i < slots.Length; i++)
        {
            if (bag.HasItem(i))
            {
                slots[i].SetSlot(i, bag.GetItem(i), this);
            }
            else
            {
                if(i < currentCount)
                {
                    slots[i].ClearSlot();
                }
                else
                {
                    slots[i].LockSlot();
                }
            }
        }

        RefreshWeightUI();
    }

    private void RefreshWeightUI()
    {
        if (bag == null)
            return;

        float currentWeight = bag.GetCurrentWeight();
        float maxWeight = bag.GetMaxWeight();

        if (weightSlider != null)
        {
            weightSlider.minValue = 0f;
            weightSlider.maxValue = maxWeight;
            weightSlider.value = currentWeight;
        }

        if (weightText != null)
        {
            weightText.text = currentWeight.ToString("0.#") + " / " + maxWeight.ToString("0.#");
        }
    }

    private void ClearUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].ClearSlot();
        }

        if (weightSlider != null)
        {
            weightSlider.minValue = 0f;
            weightSlider.maxValue = 1f;
            weightSlider.value = 0f;
        }

        if (weightText != null)
        {
            weightText.text = "0 / 0";
        }

        if (dragIconImage != null)
            dragIconImage.gameObject.SetActive(false);
    }

    public void ClickSlot(int slotIndex)
    {
        if (bag == null)
            return;

        EquipmentBagManager.instance.UnequipItemFromCurrentBag(slotIndex);
        bag.RefreshUI();
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (bag == null)
            return;

        Debug.Log("스왑 시도");

        bag.SwapItems(fromIndex, toIndex);
        bag.RefreshUI();
    }
}