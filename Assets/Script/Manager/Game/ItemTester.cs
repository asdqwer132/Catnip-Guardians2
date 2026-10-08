using UnityEngine;
using static UnityEditor.Progress;

[System.Serializable]
public class TestItemList
{

}
[System.Serializable]
public class TestItem
{

    [Header("Item")]
    public bool isUse = true;
    public ItemData[] item;

    [Header("Bag")]
    public EquipmentBag targetBagData;
}
public class ItemTester : MonoBehaviour
{
    public TestItem[] itemss;
    public void Init()
    {
        foreach (var items in itemss)
        {
            foreach (var test in items.item)
            {
                if (items.isUse)
                {
                    foreach (var equip in items.targetBagData.equippedItems)
                    {
                        if (equip.amount == 0)
                        {
                            equip.itemData = test;
                            equip.amount = 1;
                            break;
                        }
                    }
                }
            }
        }
    }
}
