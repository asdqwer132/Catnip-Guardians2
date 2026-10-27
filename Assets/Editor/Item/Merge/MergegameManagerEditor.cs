using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(MergegameManager))]
public sealed class MergegameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var manager = (MergegameManager)target;
        if (manager.recipeBook != null && GUILayout.Button("레시피북 그래프 열기")) MergeRecipeGraphWindow.Open(manager.recipeBook);
        if (manager.LegacyRecipes.Count > 0 && GUILayout.Button("이전 레시피 목록을 레시피북으로 옮기기"))
        {
            string path = EditorUtility.SaveFilePanelInProject("레시피북 저장", "Merge Recipe Book", "asset", "저장 위치");
            if (!string.IsNullOrEmpty(path))
            {
                var book = ScriptableObject.CreateInstance<MergeRecipeBook>();
                book.items = new List<MergeItem>(manager.mergeItems);
                foreach (var tier in manager.LegacyRecipes)
                    if (tier != null) book.tiers.Add(new MergeSeries { tier = tier.tier, itemRecipeDatas = new List<ItemRecipeData>(tier.itemRecipeDatas) });
                AssetDatabase.CreateAsset(book, path);
                Undo.RecordObject(manager, "Migrate merge recipe book");
                manager.recipeBook = book; manager.ClearLegacyRecipes();
                EditorUtility.SetDirty(manager);
                PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
                if (manager.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }
    }
}
