#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EffectVisualMigrationEditor
{
    [MenuItem("Tools/Item Effects/Migrate Selected Visuals")]
    private static void MigrateSelectedVisuals()
    {
        Object[] selected = Selection.GetFiltered(typeof(ItemEffectData), SelectionMode.DeepAssets);
        List<EffectVisualData> createdVisuals = new List<EffectVisualData>();
        int migrated = 0;
        int skipped = 0;

        for (int i = 0; i < selected.Length; i++)
        {
            ItemEffectData effect = selected[i] as ItemEffectData;
            if (effect == null || effect.visualData != null || !effect.HasLegacyVisualSettings)
            {
                skipped++;
                continue;
            }

            string sourcePath = AssetDatabase.GetAssetPath(effect);
            if (string.IsNullOrEmpty(sourcePath) || !sourcePath.StartsWith("Assets/"))
            {
                Debug.LogWarning("연출 이전: Assets 폴더의 저장된 이펙트 에셋을 선택하세요.", effect);
                skipped++;
                continue;
            }

            // 같은 이전 설정을 가진 선택 에셋은 이번 작업에서 생성한 에셋을 공유한다.
            EffectVisualData visual = FindSameVisual(createdVisuals, effect);
            if (visual == null)
            {
                visual = ScriptableObject.CreateInstance<EffectVisualData>();
                visual.impactVfxPrefab = effect.impactVfxPrefab;
                visual.impactBaseScale = effect.impactBaseScale;
                visual.scaleImpactVfxByRadius = effect.scaleImpactVfxByRadius;
                visual.useAnimatorClipLifeTime = effect.useAnimatorClipLifeTime;
                visual.impactVfxLifeTime = effect.impactVfxLifeTime;
                visual.audioSource = effect.audioSource;

                string directory = Path.GetDirectoryName(sourcePath).Replace('\\', '/');
                string baseName = Path.GetFileNameWithoutExtension(sourcePath);
                string newPath = AssetDatabase.GenerateUniqueAssetPath(
                    directory + "/" + baseName + "_Visual.asset"
                );
                AssetDatabase.CreateAsset(visual, newPath);
                if (!AssetDatabase.Contains(visual))
                {
                    Object.DestroyImmediate(visual);
                    Debug.LogWarning("연출 에셋을 만들지 못해 기존 설정을 유지했습니다.", effect);
                    skipped++;
                    continue;
                }
                EditorUtility.SetDirty(visual);
                createdVisuals.Add(visual);
            }

            Undo.RecordObject(effect, "Migrate Effect Visual");
            effect.visualData = visual;
            effect.ClearLegacyVisualSettings();
            EditorUtility.SetDirty(effect);
            migrated++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("연출 이전 완료: 이펙트 " + migrated + "개, 새 공유 연출 " +
            createdVisuals.Count + "개, 건너뜀 " + skipped + "개.");
    }

    private static EffectVisualData FindSameVisual(List<EffectVisualData> visuals, ItemEffectData effect)
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            EffectVisualData visual = visuals[i];
            if (visual.impactVfxPrefab == effect.impactVfxPrefab &&
                visual.impactBaseScale.Equals(effect.impactBaseScale) &&
                visual.scaleImpactVfxByRadius == effect.scaleImpactVfxByRadius &&
                visual.useAnimatorClipLifeTime == effect.useAnimatorClipLifeTime &&
                visual.impactVfxLifeTime.Equals(effect.impactVfxLifeTime) &&
                visual.audioSource == effect.audioSource)
                return visual;
        }
        return null;
    }
}
#endif
