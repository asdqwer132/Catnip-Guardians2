#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EnemyPatternCsvImporter
{
    private const string DefaultSaveFolder = "Assets/GameData/EnemyPatterns";

    [MenuItem("Tools/Game/Enemy Pattern/Import Enemy Pattern CSV")]
    public static void ImportCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Enemy Pattern CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        string saveFolder = EditorUtility.SaveFolderPanel("Save EnemyPatternData assets", Application.dataPath, "EnemyPatterns");
        if (string.IsNullOrEmpty(saveFolder))
            return;

        string assetFolder = ToAssetPath(saveFolder);
        if (string.IsNullOrEmpty(assetFolder))
        {
            Debug.LogError("Save folder must be inside this Unity project's Assets folder.");
            return;
        }

        Directory.CreateDirectory(saveFolder);

        string[] lines = File.ReadAllLines(csvPath);
        int createdCount = 0;

        for (int i = 2; i < lines.Length; i++)
        {
            List<string> cells = SplitCsvLine(lines[i]);
            if (cells.Count < 13)
                continue;

            string enemyId = GetCell(cells, 0);
            string enemyName = GetCell(cells, 3);

            if (string.IsNullOrWhiteSpace(enemyId) || string.IsNullOrWhiteSpace(enemyName))
                continue;

            EnemyPatternData data = ScriptableObject.CreateInstance<EnemyPatternData>();
            data.name = enemyId + "_" + SanitizeFileName(enemyName) + "_PatternData";
            data.random1Interval = 4f;
            data.random2Interval = 7f;
            data.useRandom2OnlyBelowHp = false;
            data.random2HpRatio = 0.5f;

            AddPattern(data, 1, GetCell(cells, 5), GetCell(cells, 6));
            AddPattern(data, 2, GetCell(cells, 7), GetCell(cells, 8));
            AddPattern(data, 3, GetCell(cells, 9), GetCell(cells, 10));
            AddPattern(data, 4, GetCell(cells, 11), GetCell(cells, 12));

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{assetFolder}/{data.name}.asset");
            AssetDatabase.CreateAsset(data, assetPath);
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EnemyPatternCsvImporter] Created {createdCount} EnemyPatternData assets.");
    }

    [MenuItem("Tools/Game/Enemy Pattern/Create Folder")]
    public static void CreateDefaultFolder()
    {
        EnsureAssetFolder(DefaultSaveFolder);
        AssetDatabase.Refresh();
    }

    private static void AddPattern(EnemyPatternData data, int index, string conditionText, string effectText)
    {
        if (data == null)
            return;

        if (string.IsNullOrWhiteSpace(effectText))
            return;

        EnemyPatternInfo info = new EnemyPatternInfo();
        info.patternName = $"Pattern {index}";
        info.memo = effectText;
        info.enabled = true;
        info.pickGroup = ParsePickGroup(conditionText);
        info.conditionType = InferConditionType(effectText);
        info.weight = 1f;
        info.chance = effectText.Contains("낮은 확률") ? 0.25f : 1f;
        info.cooldown = index <= 2 ? 3f : 6f;
        info.blockDefaultAI = true;
        info.hpRatio = InferHpRatio(effectText);
        info.distance = 2f;

        ApplyActionPreset(info, effectText);
        data.patterns.Add(info);
    }

    private static EnemyPatternPickGroup ParsePickGroup(string conditionText)
    {
        if (conditionText.Contains("랜덤 2"))
            return EnemyPatternPickGroup.Random2;
        if (conditionText.Contains("랜덤 1"))
            return EnemyPatternPickGroup.Random1;
        return EnemyPatternPickGroup.Random1;
    }

    private static EnemyPatternConditionType InferConditionType(string effectText)
    {
        if (ContainsAny(effectText, "죽은 뒤", "죽을 때", "죽기 직전"))
            return EnemyPatternConditionType.OnLethalDamage;

        if (ContainsAny(effectText, "공격받으면", "아이템이 날아오면", "아이템에 맞으면"))
            return EnemyPatternConditionType.AfterDamaged;

        if (ContainsAny(effectText, "체력 50%", "체력 50％", "체력이 낮아지면", "체력 낮"))
            return EnemyPatternConditionType.HpRatioBelow;

        return EnemyPatternConditionType.Always;
    }

    private static float InferHpRatio(string effectText)
    {
        if (ContainsAny(effectText, "30%", "30％"))
            return 0.3f;
        if (ContainsAny(effectText, "50%", "50％"))
            return 0.5f;
        if (ContainsAny(effectText, "체력이 낮아지면", "체력 낮"))
            return 0.35f;
        return 0.5f;
    }

    private static void ApplyActionPreset(EnemyPatternInfo info, string effectText)
    {
        info.telegraphTime = 0.35f;
        info.duration = 0.6f;
        info.interval = 0.2f;
        info.repeatCount = 1;
        info.speed = 5f;
        info.range = 4f;
        info.radius = 1.2f;
        info.damageMultiplier = 1f;
        info.additionalDamage = 0f;
        info.moveSpeedMultiplier = 1f;
        info.attackDamageMultiplier = 1f;
        info.attackCooldownMultiplier = 1f;
        info.attackRangeMultiplier = 1f;
        info.incomingDamageMultiplier = 1f;

        if (ContainsAny(effectText, "피해 감소", "방패", "막기", "면역"))
        {
            info.actionType = EnemyPatternActionType.DamageReductionStance;
            info.duration = ContainsAny(effectText, "면역") ? 2.5f : 2f;
            info.incomingDamageMultiplier = ContainsAny(effectText, "면역") ? 0.1f : 0.45f;
            info.blockDefaultAI = false;
            return;
        }

        if (ContainsAny(effectText, "포효", "광폭화", "이동 속도 증가", "강화", "버프"))
        {
            info.actionType = EnemyPatternActionType.StatModifier;
            info.duration = 3f;
            info.moveSpeedMultiplier = 1.35f;
            info.attackCooldownMultiplier = 0.75f;
            info.attackDamageMultiplier = 1.15f;
            info.blockDefaultAI = false;
            return;
        }

        if (ContainsAny(effectText, "회복", "보호막", "주변 적"))
        {
            info.actionType = EnemyPatternActionType.SupportNearbyEnemies;
            info.radius = 3f;
            info.healAmount = ContainsAny(effectText, "크게") ? 50f : 20f;
            info.moveSpeedMultiplier = ContainsAny(effectText, "이동 속도") ? 1.25f : 1f;
            info.duration = 2f;
            return;
        }

        if (ContainsAny(effectText, "분열", "장애물", "뼈더미"))
        {
            info.actionType = EnemyPatternActionType.SpawnPrefab;
            info.spawnCount = ContainsAny(effectText, "2마리") ? 2 : 1;
            info.spawnSpreadRadius = 0.6f;
            return;
        }

        if (ContainsAny(effectText, "순간 이동", "우회", "반대편"))
        {
            info.actionType = EnemyPatternActionType.TeleportBehindTarget;
            info.teleportDistanceFromTarget = 1.2f;
            return;
        }

        if (ContainsAny(effectText, "지그재그", "좌우"))
        {
            info.actionType = EnemyPatternActionType.ZigzagMoveToTarget;
            info.duration = 1.2f;
            info.zigzagAmplitude = 0.75f;
            info.zigzagFrequency = 8f;
            return;
        }

        if (ContainsAny(effectText, "물러난", "뒤로", "구르"))
        {
            info.actionType = EnemyPatternActionType.RetreatThenJump;
            info.duration = 0.9f;
            info.radius = 1.2f;
            info.damageMultiplier = 1.15f;
            return;
        }

        if (ContainsAny(effectText, "주변을 빠르게 돌", "반 바퀴", "주변을 3연속", "십자 방향"))
        {
            info.actionType = EnemyPatternActionType.CircleThenCharge;
            info.duration = 1.2f;
            info.speed = 6f;
            info.radius = 1f;
            info.damageMultiplier = 1.2f;
            return;
        }

        if (ContainsAny(effectText, "돌진", "차지"))
        {
            info.actionType = EnemyPatternActionType.ChargeToTarget;
            info.duration = 0.55f;
            info.speed = 7f;
            info.radius = 1f;
            info.damageMultiplier = 1.2f;
            return;
        }

        if (ContainsAny(effectText, "점프", "도약", "착지"))
        {
            info.actionType = EnemyPatternActionType.JumpToTarget;
            info.duration = 0.55f;
            info.radius = 1.2f;
            info.damageMultiplier = 1.25f;
            return;
        }

        if (ContainsAny(effectText, "화살", "마법탄", "검기", "충격파", "발사"))
        {
            info.actionType = EnemyPatternActionType.RangedAttack;
            info.range = 6f;
            info.speed = 8f;
            info.duration = 1.5f;
            info.radius = 0.5f;
            info.repeatCount = ContainsAny(effectText, "3갈래", "3연속", "연속") ? 3 : ContainsAny(effectText, "2연속", "2갈래") ? 2 : 1;
            info.circleAngle = ContainsAny(effectText, "3갈래") ? 35f : 0f;
            return;
        }

        if (ContainsAny(effectText, "장판", "랜덤 위치 3곳"))
        {
            info.actionType = EnemyPatternActionType.MultiAreaAttack;
            info.repeatCount = ContainsAny(effectText, "3곳") ? 3 : 1;
            info.radius = 1.3f;
            info.range = 2f;
            info.damageMultiplier = 1.1f;
            return;
        }

        if (ContainsAny(effectText, "범위", "회전", "베기", "내려찍기", "강타", "찌르기"))
        {
            info.actionType = EnemyPatternActionType.AreaAttack;
            info.radius = 1.2f;
            info.damageMultiplier = ContainsAny(effectText, "강하게", "추가 피해") ? 1.35f : 1.1f;
            info.additionalDamage = ContainsAny(effectText, "추가 피해") ? 5f : 0f;
            return;
        }

        info.actionType = EnemyPatternActionType.None;
        info.blockDefaultAI = false;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int i = 0; i < keywords.Length; i++)
        {
            if (text.Contains(keywords[i]))
                return true;
        }

        return false;
    }

    private static string GetCell(List<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count)
            return string.Empty;

        return cells[index].Trim();
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string cell = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cell += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(cell);
                cell = string.Empty;
            }
            else
            {
                cell += c;
            }
        }

        result.Add(cell);
        return result;
    }

    private static string ToAssetPath(string absolutePath)
    {
        absolutePath = absolutePath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (!absolutePath.StartsWith(dataPath))
            return string.Empty;

        return "Assets" + absolutePath.Substring(dataPath.Length);
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalid.ToString(), string.Empty);

        return fileName.Replace(" ", "_");
    }
}
#endif
