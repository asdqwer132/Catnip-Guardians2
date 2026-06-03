using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AllBagInBag : BagUIBase
{
    public TMP_Text count;
    protected override void RefreshUI(EquipmentBag bag)
    {
        count.text = "" + bag.name;
    }
}
