using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentBagPresetPanelUI : MonoBehaviour
{
    [Header("Button")]
    public Button addPresetButton;

    [Header("List")]
    public Transform contentParent;
    public EquipmentBagPresetSlotUI presetSlotPrefab;

    [Header("Drag")]
    [Tooltip("비워두면 Root Canvas 아래에 드래그 복사본을 만듭니다.")]
    public Transform dragGhostParent;

    private EquipmentBagPresetManager manager;
    private readonly List<EquipmentBagPresetSlotUI> slotUIs = new List<EquipmentBagPresetSlotUI>();

    public Transform DragGhostParent => dragGhostParent;

    public void Init(EquipmentBagPresetManager manager)
    {
        this.manager = manager;

        if (addPresetButton != null)
        {
            addPresetButton.onClick.RemoveListener(OnClickAddPresetButton);
            addPresetButton.onClick.AddListener(OnClickAddPresetButton);
        }
    }

    private void OnDestroy()
    {
        if (addPresetButton != null)
            addPresetButton.onClick.RemoveListener(OnClickAddPresetButton);
    }

    public void Refresh(List<EquipmentBagPreset> presets)
    {
        ClearSlots();

        if (manager == null)
            return;

        if (contentParent == null || presetSlotPrefab == null)
            return;

        if (presets == null)
            return;

        for (int i = 0; i < presets.Count; i++)
        {
            EquipmentBagPresetSlotUI slotUI = Instantiate(presetSlotPrefab, contentParent);
            slotUI.SetSlot(manager, i, presets[i]);
            slotUIs.Add(slotUI);
        }
    }

    public int GetInsertIndex(PointerEventData eventData, EquipmentBagPresetSlotUI draggingSlot)
    {
        if (contentParent == null)
            return -1;

        int maxCount = manager != null ? manager.PresetCount : slotUIs.Count;

        if (maxCount <= 0)
            return 0;

        bool horizontal = IsHorizontalList();
        Camera eventCamera = GetEventCamera(eventData);
        int insertIndex = maxCount;

        for (int i = 0; i < contentParent.childCount; i++)
        {
            Transform child = contentParent.GetChild(i);

            if (child == null)
                continue;

            EquipmentBagPresetSlotUI slotUI = child.GetComponent<EquipmentBagPresetSlotUI>();

            if (slotUI == null)
                continue;

            RectTransform childRect = child as RectTransform;

            if (childRect == null)
                continue;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(childRect, eventData.position, eventCamera, out localPoint);

            if (horizontal)
            {
                float middleX = (childRect.rect.xMin + childRect.rect.xMax) * 0.5f;

                if (localPoint.x < middleX)
                    return Mathf.Clamp(slotUI.PresetIndex, 0, maxCount);

                insertIndex = slotUI.PresetIndex + 1;
            }
            else
            {
                float middleY = (childRect.rect.yMin + childRect.rect.yMax) * 0.5f;

                // 세로 리스트 기준: 슬롯 중심보다 위면 해당 슬롯 앞, 아래면 다음 위치.
                if (localPoint.y > middleY)
                    return Mathf.Clamp(slotUI.PresetIndex, 0, maxCount);

                insertIndex = slotUI.PresetIndex + 1;
            }
        }

        return Mathf.Clamp(insertIndex, 0, maxCount);
    }

    private bool IsHorizontalList()
    {
        if (contentParent == null)
            return false;

        HorizontalLayoutGroup horizontalLayoutGroup = contentParent.GetComponent<HorizontalLayoutGroup>();
        VerticalLayoutGroup verticalLayoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();

        return horizontalLayoutGroup != null && verticalLayoutGroup == null;
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (eventData != null && eventData.pressEventCamera != null)
            return eventData.pressEventCamera;

        if (eventData != null)
            return eventData.enterEventCamera;

        return null;
    }

    private void OnClickAddPresetButton()
    {
        if (manager == null)
            return;

        manager.AddCurrentBagPreset();
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
                Destroy(slotUIs[i].gameObject);
        }

        slotUIs.Clear();

        if (contentParent == null)
            return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}
