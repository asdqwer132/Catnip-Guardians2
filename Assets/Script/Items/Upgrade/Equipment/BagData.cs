using UnityEngine;

[CreateAssetMenu(fileName = "Bag", menuName = "GameData/Items/Bag")]
public class BagData : DefaultData
{
    public int slotCount = 1;
    public int maxWeight;
}
