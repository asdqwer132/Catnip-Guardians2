using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BagSelectManager : MonoBehaviour
{
    [Header("Bag Managers")]
    public BagItemUseManager[] bagUseManagers;
    public GameObject[] toggles;

    [Header("Default")]
    public int currentBagIndex = 0;

    public event Action<int> OnBagSelected;

    public int CurrentBagIndex => currentBagIndex;
    public EquipmentBag GetCurrentBag()
    {
        return bagUseManagers[currentBagIndex].bag;
    }

    public BagItemUseManager CurrentBagUseManager
    {
        get
        {
            if (bagUseManagers == null)
                return null;

            if (currentBagIndex < 0 || currentBagIndex >= bagUseManagers.Length)
                return null;

            return bagUseManagers[currentBagIndex];
        }
    }

    public void SetToggles(GameObject[] toggles)
    {
        this.toggles = toggles;
    }

    public void Init()
    {
        RefreshUI();
        SelectBag(currentBagIndex);
    }

    public void HandleBagSelectInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectBag(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectBag(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectBag(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectBag(3);
    }

    public void SelectBag(int index)
    {
        if (bagUseManagers == null || bagUseManagers.Length == 0)
        {
            Debug.LogWarning("등록된 가방 매니저가 없습니다.");
            return;
        }

        if (index < 0 || index >= bagUseManagers.Length)
        {
            Debug.LogWarning("잘못된 가방 번호: " + index);
            return;
        }

        BagItemUseManager targetManager = bagUseManagers[index];

        if (targetManager == null)
        {
            Debug.LogWarning("해당 가방 매니저가 비어있습니다: " + index);
            return;
        }

        if (!UnlockCheckUtility.CanUse(targetManager.GetBagData()))
            return;

        if (currentBagIndex == index)
        {
            OnBagSelected?.Invoke(currentBagIndex);
            return;
        }

        currentBagIndex = index;
        OnBagSelected?.Invoke(currentBagIndex);
    }

    private void RefreshUI()
    {
        if (bagUseManagers == null || toggles == null)
            return;

        int count = Mathf.Min(bagUseManagers.Length, toggles.Length);

        for (int i = 0; i < count; i++)
        {
            if (toggles[i] == null || bagUseManagers[i] == null)
                continue;

            toggles[i].SetActive(UnlockCheckUtility.CanUse(bagUseManagers[i].GetBagData()));
        }
    }

    public BagItemUseManager GetBagUseManager(int index)
    {
        if (bagUseManagers == null)
            return null;

        if (index < 0 || index >= bagUseManagers.Length)
            return null;

        return bagUseManagers[index];
    }

    public int GetBagCount()
    {
        if (bagUseManagers == null)
            return 0;

        return bagUseManagers.Length;
    }

    public void ResetCurrentBagUsePosition()
    {
        if (CurrentBagUseManager == null)
            return;

        CurrentBagUseManager.ResetUsePosition();
    }

    public void ResetAllBagUsePositions()
    {
        if (bagUseManagers == null)
            return;

        for (int i = 0; i < bagUseManagers.Length; i++)
        {
            if (bagUseManagers[i] == null)
                continue;

            bagUseManagers[i].ResetUsePosition();
        }
    }

    public void ResetAllCooldowns()
    {
        if (bagUseManagers == null)
            return;

        for (int i = 0; i < bagUseManagers.Length; i++)
        {
            if (bagUseManagers[i] == null)
                continue;

            bagUseManagers[i].ResetAllCooldowns();
        }
    }
}