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

    // Older round snapshots did not store unlock keys or the starter rewards.
    public static List<string> RestoreLegacy(IEnumerable<SkillNodeData> nodes)
    {
        var keys = new List<string> { BoxKey(1), SkillKey(1, 1), EggKey(1) };
        if (nodes == null) return keys;
        foreach (var node in nodes)
        {
            if (node?.rewards == null) continue;
            foreach (var reward in node.rewards)
            {
                if (reward is not UnlockSkillReward unlock) continue;
                if (unlock.UnlockType == DataType.System) keys.Add(unlock.UnlockId);
                else AddLegacyKey(keys, unlock.UnlockType, unlock.UnlockId);
            }
        }
        return keys;
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
