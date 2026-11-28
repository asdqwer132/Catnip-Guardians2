using System.Collections.Generic;

public static class EquipmentUnlocks
{
    public const int BoxCount = 8;

    internal static string BoxKey(int box) => $"GimickOpen_{box}";
    internal static string SkillKey(int box, int skill) => $"Gimick{skill}SkillOpen_{box}";
    internal static string EggKey(int box) => $"TreeOpen_{box}";

    public static List<string> Capture(UnlockManager manager)
    {
        var keys = manager.GetUnlockedIds(DataType.System);
        foreach (string id in manager.GetUnlockedIds(DataType.MergeBox))
            AddLegacyKey(keys, DataType.MergeBox, id);
        foreach (string id in manager.GetUnlockedIds(DataType.BoardSkill))
            AddLegacyKey(keys, DataType.BoardSkill, id);
        return keys;
    }

    public static List<string> Restore(IEnumerable<SkillNodeData> nodes)
    {
        var keys = new List<string>();
        var defaults = DefaultUnlockRewards.Load();
        if (defaults != null)
            foreach (var reward in defaults.rewards)
                AddReward(keys, reward);
        if (nodes == null) return keys;
        foreach (var node in nodes)
        {
            if (node?.rewards == null) continue;
            foreach (var reward in node.rewards)
                AddReward(keys, reward as UnlockSkillReward);
        }
        return keys;
    }

    private static void AddReward(List<string> keys, UnlockSkillReward reward)
    {
        if (reward == null || string.IsNullOrWhiteSpace(reward.UnlockId)) return;
        if (reward.UnlockType == DataType.System)
        {
            if (!keys.Contains(reward.UnlockId)) keys.Add(reward.UnlockId);
        }
        else AddLegacyKey(keys, reward.UnlockType, reward.UnlockId);
    }

    private static void AddLegacyKey(List<string> keys, DataType type, string id)
    {
        if (type == DataType.MergeBox && int.TryParse(id, out int box) && id == box.ToString())
            keys.Add(BoxKey(box));
        if (type == DataType.BoardSkill && id != null)
        {
            var parts = id.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out box) &&
                int.TryParse(parts[1], out int skill) && id == $"{box}:{skill}")
                keys.Add(SkillKey(box, skill));
        }
    }
}
