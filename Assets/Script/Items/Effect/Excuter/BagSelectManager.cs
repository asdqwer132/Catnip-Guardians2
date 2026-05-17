using UnityEngine;

public class BagSelectManager : MonoBehaviour
{
    [Header("Bag Managers")]
    public BagItemUseManager[] bagUseManagers;

    [Header("Default")]
    public int currentBagIndex = 0;

    public int CurrentBagIndex
    {
        get { return currentBagIndex; }
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

    public void Init()
    {
        SelectBag(currentBagIndex);
    }

    public void HandleBagSelectInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectBag(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectBag(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SelectBag(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SelectBag(3);
    }

    public bool SelectBag(int index)
    {
        if (bagUseManagers == null || bagUseManagers.Length == 0)
        {
            Debug.LogWarning("등록된 가방 매니저가 없습니다.");
            return false;
        }

        if (index < 0 || index >= bagUseManagers.Length)
        {
            Debug.LogWarning("잘못된 가방 번호: " + index);
            return false;
        }

        if (bagUseManagers[index] == null)
        {
            Debug.LogWarning("해당 가방 매니저가 비어있습니다: " + index);
            return false;
        }

        currentBagIndex = index;

        Debug.Log("현재 선택된 가방 인덱스: " + currentBagIndex);

        return true;
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