using UnityEngine;

[CreateAssetMenu(fileName = "Board Pin", menuName = "Items/Board Pin")]
public sealed class BoardPinData : ItemData
{
    [Tooltip("Optional sprite shown in an equipped board cell. The inventory uses the basic icon.")]
    public Sprite boardIcon;

    private void OnValidate() => dataType = DataType.Pin;
}
