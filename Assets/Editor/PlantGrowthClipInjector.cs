using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class PlantGrowthClipInjector
{
    private const string PlantFolder = "Assets/Data/Scriptable/MainPlant";
    private const string ClipFolder = "Assets/Animation/Player/Tree";

    // Tree2_0_idle / Tree2_0_hurt use stage 0 for the seed.
    private static readonly Regex StageClip = new Regex(@"(\d+)\D*(idle|hurt)$", RegexOptions.IgnoreCase);

    [MenuItem("Tools/Utility/Animation/Inject Tree Growth Clips")]
    private static void Inject()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:PlantData", new[] { PlantFolder }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var treeNumber = Regex.Match(Path.GetFileNameWithoutExtension(assetPath), @"\d+$");
            string folder = $"{ClipFolder}/Tree ({treeNumber.Value})";
            if (!treeNumber.Success || !AssetDatabase.IsValidFolder(folder)) continue;

            var plant = AssetDatabase.LoadAssetAtPath<PlantData>(assetPath);
            foreach (string clipGuid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(clipGuid);
                var match = StageClip.Match(Path.GetFileNameWithoutExtension(clipPath));
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out int index) ||
                    index >= plant.growthVisuals.Length || plant.growthVisuals[index] == null) continue;

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null) continue;
                var stage = plant.growthVisuals[index];
                Undo.RecordObject(stage, "Inject Tree Growth Clips");
                if (match.Groups[2].Value.ToLowerInvariant() == "idle") stage.idle = clip;
                else stage.hurt = clip;
                EditorUtility.SetDirty(stage);
            }
        }

        AssetDatabase.SaveAssets();
    }
}
