#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class EnemyPatternActionCsvImporter
{
    private const string MenuPath = "Tools/Enemy/Enemy Pattern/Import Action Pattern CSV";
    private const string DefaultOutputFolder = "Assets/GameData/EnemyPatterns";

    [MenuItem(MenuPath)]
    public static void ImportCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Enemy Pattern CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        EnsureFolder(DefaultOutputFolder);

        List<List<string>> rows = ReadCsv(csvPath);
        if (rows == null || rows.Count < 3)
        {
            Debug.LogWarning("CSV 행이 부족합니다.");
            return;
        }

        int createdCount = 0;

        for (int i = 2; i < rows.Count; i++)
        {
            List<string> row = rows[i];
            if (row == null || row.Count < 13)
                continue;

            string enemyId = GetCell(row, 0);
            string series = GetCell(row, 1);
            string enemyClass = GetCell(row, 2);
            string enemyName = GetCell(row, 3);
            string role = GetCell(row, 4);

            if (string.IsNullOrWhiteSpace(enemyName))
                continue;

            EnemyPatternSetData data = ScriptableObject.CreateInstance<EnemyPatternSetData>();
            data.name = $"PatternSet_{enemyId}_{enemyName}";
            data.patternCooldown = GetDefaultPatternCooldown(enemyClass);
            data.useRandom2OnlyBelowHp = enemyClass.Contains("Boss");
            data.random2HpRatio = 0.65f;
            data.showLog = false;

            string fileName = SanitizeFileName($"{enemyId}_{enemyName}_PatternSet.asset");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{DefaultOutputFolder}/{fileName}");
            AssetDatabase.CreateAsset(data, assetPath);

            for (int patternIndex = 0; patternIndex < 4; patternIndex++)
            {
                int conditionIndex = 5 + patternIndex * 2;
                int effectIndex = conditionIndex + 1;

                string conditionText = GetCell(row, conditionIndex);
                string effectText = GetCell(row, effectIndex);

                if (string.IsNullOrWhiteSpace(conditionText) && string.IsNullOrWhiteSpace(effectText))
                    continue;

                EnemyPatternEntry entry = CreateEntry(
                    data,
                    enemyName,
                    role,
                    patternIndex + 1,
                    conditionText,
                    effectText
                );

                data.patterns.Add(entry);
            }

            EditorUtility.SetDirty(data);
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Enemy Pattern Action CSV Import 완료: {createdCount}개 생성 / 경로: {DefaultOutputFolder}");
    }

    private static EnemyPatternEntry CreateEntry(EnemyPatternSetData owner, string enemyName, string role, int index, string conditionText, string effectText)
    {
        EnemyPatternEntry entry = new EnemyPatternEntry
        {
            patternName = $"{enemyName}_Pattern{index}",
            memo = $"Role: {role}\nCondition: {conditionText}\nEffect: {effectText}",
            enabled = true,
            pickGroup = GetPickGroup(conditionText, index),
            blockDefaultAI = ShouldBlockDefaultAI(effectText),
            weight = index <= 2 ? 2f : 1f,
            chance = 1f,
            cooldown = 0f,
            consumeOnce = false
        };

        AddConditions(entry, conditionText, effectText);
        AddActions(owner, entry, index, effectText);

        return entry;
    }

    private static void AddConditions(EnemyPatternEntry entry, string conditionText, string effectText)
    {
        if (ContainsAny(conditionText, "체력", "낮아", "HP", "hp") || ContainsAny(effectText, "체력이", "광폭화"))
        {
            entry.conditions.Add(new EnemyPatternCondition
            {
                conditionType = EnemyPatternConditionType.HpRatioBelow,
                hpRatio = 0.5f
            });
        }

        if (ContainsAny(conditionText, "공격받", "맞으면", "피격"))
        {
            entry.pickGroup = EnemyPatternPickGroup.Reactive;
            entry.conditions.Add(new EnemyPatternCondition
            {
                conditionType = EnemyPatternConditionType.AfterDamaged
            });
        }

        if (ContainsAny(conditionText, "죽", "사망") || ContainsAny(effectText, "죽기 직전", "죽을 때", "사망"))
        {
            entry.pickGroup = EnemyPatternPickGroup.Death;
            entry.consumeOnce = true;
            entry.conditions.Add(new EnemyPatternCondition
            {
                conditionType = EnemyPatternConditionType.OnLethalDamage
            });
        }

        if (entry.conditions.Count == 0)
        {
            entry.conditions.Add(new EnemyPatternCondition
            {
                conditionType = EnemyPatternConditionType.Always
            });
        }
    }

    private static void AddActions(EnemyPatternSetData owner, EnemyPatternEntry entry, int index, string effectText)
    {
        string effect = effectText ?? string.Empty;

        bool isBasicOnly = ContainsAny(effect, "단순 직선 이동", "메인 식물을 향해 단순", "근접 공격", "기본 공격") &&
                           !ContainsAny(effect, "강하게", "잠깐", "돌진", "지그재그", "점프", "화살", "마법", "검기", "충격파", "장판", "포효", "방패", "회복", "소환", "분열", "순간 이동");

        if (isBasicOnly)
        {
            entry.blockDefaultAI = false;
            return;
        }

        if (NeedsTelegraph(effect))
            entry.actions.Add(CreateTelegraphAction(owner, entry, effect));

        if (ContainsAny(effect, "후퇴", "뒤로"))
            entry.actions.Add(CreateRetreatAction(owner, entry, effect));

        if (ContainsAny(effect, "순간 이동", "순간이동", "텔레포트"))
            entry.actions.Add(CreateTeleportAction(owner, entry, effect));

        if (ContainsAny(effect, "반 바퀴", "주변을", "곡선 경로", "곡선"))
            entry.actions.Add(CreateCircleAction(owner, entry, effect));

        if (ContainsAny(effect, "지그재그", "좌우"))
            entry.actions.Add(CreateZigzagAction(owner, entry, effect));

        if (ContainsAny(effect, "점프", "도약", "착지"))
            entry.actions.Add(CreateJumpAction(owner, entry, effect));
        else if (ContainsAny(effect, "돌진", "강타", "내려찍기", "들이받"))
            entry.actions.Add(CreateChargeAction(owner, entry, effect));

        if (ContainsAny(effect, "화살", "마법탄", "검기", "충격파", "발사"))
            entry.actions.Add(CreateProjectileAction(owner, entry, effect));

        if (ContainsAny(effect, "장판", "바닥", "랜덤 위치", "폭발", "착지 공격"))
            entry.actions.Add(CreateAreaDamageAction(owner, entry, effect));

        if (ContainsAny(effect, "방패", "피해 감소", "막기", "자세"))
            entry.actions.Add(CreateDamageReductionAction(owner, entry, effect));

        if (ContainsAny(effect, "포효", "광폭", "이동 속도", "공격 준비", "속도 증가", "돌진 간격 감소"))
            entry.actions.Add(CreateBuffAction(owner, entry, effect));

        if (ContainsAny(effect, "회복", "치유", "보호막", "성가"))
            entry.actions.Add(CreateHealAction(owner, entry, effect));

        if (ContainsAny(effect, "소환", "분열", "장애물", "뼈더미", "생성"))
            entry.actions.Add(CreateSpawnAction(owner, entry, effect));

        if (entry.actions.Count == 0)
        {
            EnemyWaitAction wait = CreateSubAction<EnemyWaitAction>(owner, entry, "Wait");
            wait.duration = 0.25f;
            wait.stopMove = false;
            entry.blockDefaultAI = false;
            entry.actions.Add(wait);
        }
    }

    private static EnemyTelegraphAction CreateTelegraphAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyTelegraphAction action = CreateSubAction<EnemyTelegraphAction>(owner, entry, "Telegraph");
        action.duration = ContainsAny(effect, "강하게", "큰", "보스", "연속") ? 0.7f : 0.4f;
        action.pointType = ContainsAny(effect, "랜덤 위치", "장판") ? EnemyPatternPointType.Target : EnemyPatternPointType.Self;
        action.randomRadius = 1.5f;
        action.distance = 1f;
        return action;
    }

    private static EnemyRetreatAction CreateRetreatAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyRetreatAction action = CreateSubAction<EnemyRetreatAction>(owner, entry, "Retreat");
        action.speed = 3.5f;
        action.duration = 0.35f;
        return action;
    }

    private static EnemyTeleportAction CreateTeleportAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyTeleportAction action = CreateSubAction<EnemyTeleportAction>(owner, entry, "Teleport");
        action.distanceFromTarget = 1f;
        return action;
    }

    private static EnemyCircleMoveAction CreateCircleAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyCircleMoveAction action = CreateSubAction<EnemyCircleMoveAction>(owner, entry, "CircleMove");
        action.duration = 0.9f;
        action.angle = ContainsAny(effect, "반 바퀴") ? 180f : 120f;
        action.radius = 1.4f;
        return action;
    }

    private static EnemyZigzagMoveAction CreateZigzagAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyZigzagMoveAction action = CreateSubAction<EnemyZigzagMoveAction>(owner, entry, "Zigzag");
        action.speed = 3.5f;
        action.duration = 0.9f;
        action.amplitude = 0.8f;
        action.frequency = 8f;
        return action;
    }

    private static EnemyJumpAction CreateJumpAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyJumpAction action = CreateSubAction<EnemyJumpAction>(owner, entry, "Jump");
        action.duration = ContainsAny(effect, "짧게") ? 0.4f : 0.55f;
        action.endDistanceFromTarget = 0.6f;
        action.visualArcHeight = 0.35f;
        action.hitRadius = ContainsAny(effect, "큰", "강") ? 1.3f : 1f;
        action.damageMultiplier = ContainsAny(effect, "강", "큰") ? 1.5f : 1.25f;
        return action;
    }

    private static EnemyChargeAction CreateChargeAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyChargeAction action = CreateSubAction<EnemyChargeAction>(owner, entry, "Charge");
        action.speed = ContainsAny(effect, "짧은") ? 5.5f : 7f;
        action.duration = ContainsAny(effect, "짧은") ? 0.45f : 0.65f;
        action.hitRadius = ContainsAny(effect, "강타", "내려찍기") ? 1f : 0.75f;
        action.damageMultiplier = ContainsAny(effect, "강", "내려찍기") ? 1.5f : 1.2f;
        return action;
    }

    private static EnemyProjectileAction CreateProjectileAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyProjectileAction action = CreateSubAction<EnemyProjectileAction>(owner, entry, "Projectile");
        action.speed = ContainsAny(effect, "마법") ? 5f : 7f;
        action.lifeTime = 3f;
        action.damageMultiplier = ContainsAny(effect, "관통", "성스러운") ? 1.2f : 1f;
        action.count = ExtractCount(effect, 1);
        action.interval = ContainsAny(effect, "연속") ? 0.18f : 0.08f;
        action.spreadAngle = action.count >= 3 ? 35f : action.count == 2 ? 25f : 0f;
        action.spawnDistance = 0.35f;
        return action;
    }

    private static EnemyAreaDamageAction CreateAreaDamageAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyAreaDamageAction action = CreateSubAction<EnemyAreaDamageAction>(owner, entry, "AreaDamage");
        action.pointType = ContainsAny(effect, "랜덤") ? EnemyPatternPointType.RandomAroundTarget : EnemyPatternPointType.Target;
        action.radius = ContainsAny(effect, "큰", "강") ? 1.5f : 1f;
        action.damageMultiplier = ContainsAny(effect, "강", "폭발") ? 1.4f : 1.1f;
        action.count = ContainsAny(effect, "3곳", "세 곳") ? 3 : ExtractCount(effect, 1);
        action.interval = 0.15f;
        action.randomRadius = 2f;
        return action;
    }

    private static EnemyStatModifierAction CreateDamageReductionAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyStatModifierAction action = CreateSubAction<EnemyStatModifierAction>(owner, entry, "DamageReduction");
        action.duration = 1.5f;
        action.incomingDamageMultiplier = 0.45f;
        action.waitUntilEnd = true;
        return action;
    }

    private static EnemyStatModifierAction CreateBuffAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyStatModifierAction action = CreateSubAction<EnemyStatModifierAction>(owner, entry, "Buff");
        action.duration = ContainsAny(effect, "광폭") ? 4f : 2.5f;
        action.moveSpeedMultiplier = ContainsAny(effect, "이동", "광폭", "속도") ? 1.35f : 1f;
        action.attackDamageMultiplier = ContainsAny(effect, "광폭") ? 1.25f : 1f;
        action.attackCooldownMultiplier = ContainsAny(effect, "공격 준비", "돌진 간격") ? 0.75f : 1f;
        action.waitUntilEnd = false;
        return action;
    }

    private static EnemyHealNearbyAction CreateHealAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemyHealNearbyAction action = CreateSubAction<EnemyHealNearbyAction>(owner, entry, "HealNearby");
        action.radius = 2f;
        action.healAmount = ContainsAny(effect, "큰") ? 15f : 8f;
        action.includeSelf = true;
        return action;
    }

    private static EnemySpawnPrefabAction CreateSpawnAction(EnemyPatternSetData owner, EnemyPatternEntry entry, string effect)
    {
        EnemySpawnPrefabAction action = CreateSubAction<EnemySpawnPrefabAction>(owner, entry, "Spawn");
        action.pointType = EnemyPatternPointType.RandomAroundSelf;
        action.count = ContainsAny(effect, "분열") ? 2 : ExtractCount(effect, 1);
        action.spreadRadius = 0.6f;
        action.interval = 0.08f;
        return action;
    }

    private static T CreateSubAction<T>(EnemyPatternSetData owner, EnemyPatternEntry entry, string suffix) where T : EnemyPatternAction
    {
        T action = ScriptableObject.CreateInstance<T>();
        action.name = $"{entry.patternName}_{suffix}";
        AssetDatabase.AddObjectToAsset(action, owner);
        EditorUtility.SetDirty(action);
        return action;
    }

    private static EnemyPatternPickGroup GetPickGroup(string conditionText, int index)
    {
        if (ContainsAny(conditionText, "랜덤 2"))
            return EnemyPatternPickGroup.Random2;

        if (ContainsAny(conditionText, "랜덤 1"))
            return EnemyPatternPickGroup.Random1;

        return index <= 2 ? EnemyPatternPickGroup.Random1 : EnemyPatternPickGroup.Random2;
    }

    private static bool ShouldBlockDefaultAI(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect))
            return false;

        if (ContainsAny(effect, "단순 직선 이동", "단순 이동", "근접 공격", "기본 공격") &&
            !ContainsAny(effect, "강하게", "돌진", "점프", "화살", "마법", "검기", "충격파", "장판", "포효", "방패", "회복", "소환", "분열", "지그재그"))
            return false;

        return true;
    }

    private static bool NeedsTelegraph(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect))
            return false;

        return ContainsAny(effect, "예고", "돌진", "점프", "도약", "강타", "내려찍기", "장판", "마법", "검기", "충격파", "화살", "포효", "방패");
    }

    private static int ExtractCount(string text, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text))
            return defaultValue;

        if (text.Contains("3갈래") || text.Contains("3개") || text.Contains("3곳") || text.Contains("세"))
            return 3;

        if (text.Contains("2갈래") || text.Contains("2개") || text.Contains("2연속") || text.Contains("두"))
            return 2;

        if (text.Contains("연속"))
            return 2;

        return defaultValue;
    }

    private static float GetDefaultPatternCooldown(string enemyClass)
    {
        if (enemyClass.Contains("Boss"))
            return 3.5f;

        if (enemyClass.Contains("MiniBoss"))
            return 4.2f;

        return 5f;
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        if (string.IsNullOrEmpty(source))
            return false;

        for (int i = 0; i < values.Length; i++)
        {
            if (source.Contains(values[i]))
                return true;
        }

        return false;
    }

    private static string GetCell(List<string> row, int index)
    {
        if (row == null || index < 0 || index >= row.Count)
            return string.Empty;

        return row[index]?.Trim() ?? string.Empty;
    }

    private static string SanitizeFileName(string fileName)
    {
        string invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidPattern = $"[{invalid}]";
        return Regex.Replace(fileName, invalidPattern, "_");
    }

    private static void EnsureFolder(string assetFolder)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetFolder);
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
    }

    private static List<List<string>> ReadCsv(string path)
    {
        string text = File.ReadAllText(path, Encoding.UTF8);
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuote = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuote && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuote = !inQuote;
                }
                continue;
            }

            if (c == ',' && !inQuote)
            {
                row.Add(cell.ToString());
                cell.Length = 0;
                continue;
            }

            if ((c == '\n' || c == '\r') && !inQuote)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                row.Add(cell.ToString());
                cell.Length = 0;

                bool hasValue = false;
                for (int j = 0; j < row.Count; j++)
                {
                    if (!string.IsNullOrWhiteSpace(row[j]))
                    {
                        hasValue = true;
                        break;
                    }
                }

                if (hasValue)
                    rows.Add(row);

                row = new List<string>();
                continue;
            }

            cell.Append(c);
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
#endif
