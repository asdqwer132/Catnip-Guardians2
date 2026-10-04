using UnityEngine;
using static UnityEditor.Progress;
[System.Serializable]
public class TestItem
{

    [Header("Item")]
    public bool isUse = true;
    public ItemData item;

    [Header("Bag")]
    public EquipmentBag targetBagData;
}
public class ItemTester : MonoBehaviour
{
    public TestItem[] items;
    public void Init()
    {
        foreach (var test in items)
        {
            if (test.isUse)
            {
                foreach (var equip in test.targetBagData.equippedItems)
                {
                    if (equip.amount == 0)
                    {
                        equip.itemData = test.item;
                        equip.amount = 1;
                        break;
                    }
                }
            }
        }
    }
}
