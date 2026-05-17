using UnityEngine;

[CreateAssetMenu(fileName = "bag", menuName = "Game/bag")]
public class BagData : ScriptableObject
{
    public Sprite icon;
    public int maxWeight;
}
