using UnityEngine;

[CreateAssetMenu(menuName = "Entities/Player/Player Data")]
public sealed class PlayerData : DefaultData
{
    public PlayerBattleVisualData battleVisual;
    public PlantData normalPlant;
    public PlantData bossPlant;

    public PlantData GetPlant(bool bossBattle) => bossBattle && bossPlant != null ? bossPlant : normalPlant;
}
