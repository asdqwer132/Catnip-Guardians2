using UnityEngine;

[CreateAssetMenu(fileName = "Board Pin", menuName = "Items/Board Pin")]
public sealed class BoardPinData : ItemData
{
    [Tooltip("Optional sprite shown in an equipped board cell. The inventory uses the basic icon.")]
    public Sprite boardIcon;
    [Tooltip("Optional world prefab. Leave empty to use the board’s default pin object.")]
    public PinballPin worldPrefab;
    [Tooltip("Effects are run in order each time a falling item hits this pin.")]
    public BoardPinEffect[] hitEffects = System.Array.Empty<BoardPinEffect>();

    private void OnValidate() => dataType = DataType.Pin;
}
