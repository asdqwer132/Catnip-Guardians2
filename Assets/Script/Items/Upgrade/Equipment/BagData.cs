using UnityEngine;

[CreateAssetMenu(fileName = "Bag", menuName = "03.Upgrade/Equipment/Bag")]
public class BagData : DefaultData
{
    [Header("Extra Data")]
    public int slotCount = 1;
    public int maxWeight;
}
