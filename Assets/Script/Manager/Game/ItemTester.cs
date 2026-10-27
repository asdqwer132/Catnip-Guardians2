using UnityEngine;
using static UnityEditor.Progress;

[System.Serializable]
public class TestItemList { }
[System.Serializable]
public class TestItem
{
    public string name;
    [Header("Item")]
    public bool isUse = true;
    public ItemData[] item;

    [Header("Bag")]
    public EquipmentBag targetBagData;
}
public class ItemTester : MonoBehaviour
{
    public TestItem[] itemss;
    public ItemUsePositionProvider positionProvider;
    public RectTransform testerDisplayer;
    public RectTransform testerDisplayerHp;
    private bool isTest = false;
    public void Init()
    {
        foreach (var items in itemss)
        {
            foreach (var test in items.item)
            {
                if (items.isUse)
                {
                    isTest = true;
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
        if(isTest)
            SetTestMode();
    }
    private void SetTestMode()
    {
        positionProvider.selectPositionOnly = false;
        Vector2 pos = testerDisplayer.anchoredPosition;
        pos.x = 25f;
        testerDisplayer.anchoredPosition = pos;
        Vector2 poshp = testerDisplayerHp.anchoredPosition;
        pos.y = 150+25f;
        testerDisplayerHp.anchoredPosition = pos;
    }
}
