using System.Text;

public static class GameSessionSkillDiagnostics
{
    public static string Describe(UpgradeToGeneralData data)
    {
        if (data == null) return "No UpgradeToGeneralData in GameSession. Using standalone defaults.";
        var text = new StringBuilder();
        text.Append("skillPlantIndex=").Append(data.mainPlantIndex)
            .Append(", unlockedNodes=").Append(data.unlockedSkillNodes?.Count ?? 0);
        if (data.unlockedSkillNodes != null)
            foreach (var node in data.unlockedSkillNodes)
            {
                if (node == null) { text.Append("\n  Missing node asset"); continue; }
                text.Append("\n  Node=").Append(node.name).Append(", mapId=").Append(node.skillMap?.dataId ?? "none")
                    .Append(", nodeId=").Append(node.dataId).Append(", rewards=").Append(node.rewards?.Count ?? 0);
                if (node.rewards != null) foreach (var reward in node.rewards)
                {
                    text.Append("\n    Reward=").Append(reward != null ? reward.name : "MISSING");
                    if (reward is MainPlantIndexSkillReward plant) text.Append(", plantIndex=").Append(plant.plantIndex);
                }
            }
        text.Append("\n  Registered skill items=").Append(data.skillItems?.Count ?? 0);
        if (data.skillItems != null) foreach (var item in data.skillItems)
            text.Append("\n    Skill item=").Append(item?.itemData != null ? item.itemData.dataId : "MISSING");
        if (data.potionSlots != null) foreach (var entry in data.potionSlots)
            if (entry?.potion != null)
            {
                text.Append("\n  Potion slot=").Append(entry.slotIndex).Append(", locked=").Append(entry.locked)
                    .Append(", itemId=").Append(entry.potion.dataId);
                if (entry.potion.effectDatas != null) foreach (var effect in entry.potion.effectDatas)
                    if (effect is MainPlantIndexEffect plant) text.Append(", potionPlantIndex=").Append(plant.plantIndex);
            }
        return text.ToString();
    }
}
