using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// Owned by the persistent session, independent of the upgrade scene's UI/managers.
public static class UpgradeProgressSave
{
    public const string SaveFileName = "upgrade-progress.json";
    [Serializable] private sealed class LegacySkills { public System.Collections.Generic.List<string> unlockedSkillIds; }
    [Serializable] private sealed class AssetReference { public UnityEngine.Object asset; }
    private static string ReferenceJson(UnityEngine.Object asset)
    {
        string json = JsonUtility.ToJson(new AssetReference { asset = asset });
        return json.Substring(json.IndexOf(':') + 1, json.Length - json.IndexOf(':') - 2);
    }
    private static UpgradeProgressCatalog Catalog => Resources.Load<UpgradeProgressCatalog>("UpgradeProgressCatalog");
    public static void Write(UpgradeToGeneralData data, string directory = null)
    {
        var catalog = Catalog;
        if (catalog == null) { Debug.LogError("Upgrade progress catalog is missing."); return; }
        string json = JsonUtility.ToJson(data);
        foreach (var entry in catalog.entries)
            if (entry.asset != null)
                json = json.Replace(ReferenceJson(entry.asset), "{\"assetKey\":\"" + entry.key + "\"}");
        new MergeBoxUpgradeFileStore(Path.Combine(directory ?? Application.persistentDataPath, SaveFileName)).Write(json);
    }
    public static UpgradeToGeneralData Read(string directory = null)
    {
        var catalog = Catalog;
        if (catalog == null) return null;
        string path = Path.Combine(directory ?? Application.persistentDataPath, SaveFileName);
        if (File.Exists(path))
        {
            string json = Regex.Replace(File.ReadAllText(path), @"\{""assetKey"":""([^""]*)""\}", match =>
            {
                var entry = catalog.entries.Find(value => value.key == match.Groups[1].Value);
                return ReferenceJson(entry?.asset);
            });
            return JsonUtility.FromJson<UpgradeToGeneralData>(json);
        }
        // Migration: older versions persisted unlock IDs but no equipment/inventory snapshot.
        path = Path.Combine(directory ?? Application.persistentDataPath, MergeBoxUpgradeManager.SaveFileName);
        if (!File.Exists(path)) return null;
        var legacy = JsonUtility.FromJson<LegacySkills>(File.ReadAllText(path));
        var data = new UpgradeToGeneralData { legacySkillsOnly = true };
        foreach (var node in catalog.entries.Select(entry => entry.asset).OfType<SkillNodeData>())
            if (legacy?.unlockedSkillIds != null && (legacy.unlockedSkillIds.Contains(node.ProgressKey) || legacy.unlockedSkillIds.Contains(node.dataId)))
                data.unlockedSkillNodes.Add(node);
        return data;
    }
    public static void RefreshSkillValues(UpgradeToGeneralData data)
    {
        if (data.unlockedSkillNodes == null || data.unlockedSkillNodes.Count == 0) return;
        data.mainPlantIndex = -1;
        data.skillItems.Clear();
        data.boxLevels.Clear();
        foreach (var node in data.unlockedSkillNodes)
        {
            if (node == null || node.rewards == null) continue;
            foreach (var reward in node.rewards)
            {
                if (reward is MainPlantIndexSkillReward plant) data.mainPlantIndex = Math.Max(data.mainPlantIndex, plant.plantIndex);
                if (reward is BuffSkillReward buff && buff.buffItemData != null && !data.skillItems.Exists(value => value.itemData == buff.buffItemData))
                    data.skillItems.Add(new SkillItemSnapshot { itemData = buff.buffItemData });
                if (reward is MergeBoxLevelSkillReward box)
                {
                    var level = data.boxLevels.Find(value => value.series == box.series);
                    if (level == null) data.boxLevels.Add(new MergeBoxLevelSnapshot { series = box.series, level = box.level });
                    else level.level = Math.Max(level.level, box.level);
                }
            }
        }
    }
}
