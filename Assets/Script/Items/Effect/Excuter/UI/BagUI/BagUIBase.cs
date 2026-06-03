using UnityEngine;
using UnityEngine.UI;

public abstract class BagUIBase : MonoBehaviour
{
    [Header("Bag Id")]
    public string bagId;
    public Image bagIcon;

    public EquipmentBag equipmentBag;
    public BagData bagData;
    public BagData BagData { get { return bagData; } }
    public void Init()
    {
        if (EquipmentBagManager.instance == null)
        {
            ClearUI();
            return;
        }

        Init(EquipmentBagManager.instance.GetBagData(bagId));
    }

    public void Init(EquipmentBag targetBag)
    {
        equipmentBag = targetBag;

        if (equipmentBag == null)
        {
            ClearUI();
            return;
        }

        bagData = equipmentBag.bagData;
        bagIcon.sprite = equipmentBag.bagData.icon;


        RefreshUI(equipmentBag);
    }

    protected abstract void RefreshUI(EquipmentBag bag);

    protected virtual void ClearUI() { }
}