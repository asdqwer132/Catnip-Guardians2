using UnityEngine;

[CreateAssetMenu(menuName = "Catnip/Merge Box Equipment")]
public sealed class MergeBoxEquipmentData : UnlockableDefaultData
{
    [Range(1, 8)] public int boxIndex = 1;
    public PlantData plant;
    public PlayerData player;
    public Sprite representativeIcon;
    [Tooltip("Display data in the same order as the item scene gimmick skills.")]
    public BoardGimmickSkillData[] skills = System.Array.Empty<BoardGimmickSkillData>();
}
