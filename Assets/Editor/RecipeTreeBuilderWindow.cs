#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RecipeTreeBuilderWindow : EditorWindow
{
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RecipeTreeTierUI tierPrefab;
    [SerializeField] private RecipeTreeSlotUI slotPrefab;
    [SerializeField] private RecipeTreeLineUI linePrefab;
    [SerializeField] private ItemRecipeManager recipeManager;
    [SerializeField] private List<ItemRecipeData> recipes = new();

    [Header("Serialized field names")]
    [SerializeField] private string tierFieldName = "tier";
    [SerializeField] private string materialsFieldName = "materials";

    [Header("Layout")]
    [SerializeField] private float tierSpacing = 80f;

    [Header("Generated object names")]
    [SerializeField] private string generatedRootName = "Generated Recipe Tree";
    [SerializeField] private string lineRootName = "Lines";

    private SerializedObject windowData;

    [MenuItem("Tools/Recipe Tree Builder")]
    public static void Open() => GetWindow<RecipeTreeBuilderWindow>("Recipe Tree Builder");

    private void OnEnable() => windowData = new SerializedObject(this);

    private void OnGUI()
    {
        windowData.Update();
        EditorGUILayout.HelpBox(
            "Content 아래에 티어를 만들고, 각 티어 안에 슬롯을 배치한 뒤 재료 → 결과 방향으로 선을 연결합니다. " +
            "ItemRecipeData의 실제 필드명이 다르면 아래 이름만 바꾸세요.", MessageType.Info);

        Draw("contentRoot");
        Draw("tierPrefab");
        Draw("slotPrefab");
        Draw("linePrefab");
        Draw("recipeManager");
        Draw("recipes", true);
        EditorGUILayout.Space();
        Draw("tierFieldName");
        Draw("materialsFieldName");
        Draw("tierSpacing");
        Draw("generatedRootName");
        Draw("lineRootName");
        windowData.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Find All ItemRecipeData Assets"))
            FindAllRecipes();

        using (new EditorGUI.DisabledScope(!CanBuild()))
        {
            if (GUILayout.Button("Build Recipe Tree", GUILayout.Height(34f)))
                Build();
        }
    }

    private void Draw(string propertyName, bool includeChildren = false)
    {
        EditorGUILayout.PropertyField(windowData.FindProperty(propertyName), includeChildren);
    }

    private bool CanBuild()
    {
        return contentRoot != null && tierPrefab != null && slotPrefab != null &&
               linePrefab != null && recipes.Any(x => x != null);
    }

    private void FindAllRecipes()
    {
        recipes = AssetDatabase.FindAssets("t:ItemRecipeData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ItemRecipeData>)
            .Where(x => x != null)
            .OrderBy(x => x.name)
            .ToList();
        Repaint();
    }

    private void Build()
    {
        Dictionary<ItemRecipeData, RecipeInfo> infos = new();
        foreach (ItemRecipeData recipe in recipes.Where(x => x != null).Distinct())
        {
            if (!TryReadRecipe(recipe, out RecipeInfo info))
                return;
            infos[recipe] = info;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Recipe Tree");

        Transform oldRoot = contentRoot.Find(generatedRootName);
        if (oldRoot != null)
            Undo.DestroyObjectImmediate(oldRoot.gameObject);

        RectTransform generatedRoot = CreateRect(generatedRootName, contentRoot);
        generatedRoot.anchorMin = new Vector2(0f, 0f);
        generatedRoot.anchorMax = new Vector2(0f, 1f);
        generatedRoot.pivot = new Vector2(0f, 0.5f);
        generatedRoot.sizeDelta = new Vector2(0f, 0f);

        HorizontalLayoutGroup tierLayout = Undo.AddComponent<HorizontalLayoutGroup>(generatedRoot.gameObject);
        tierLayout.spacing = tierSpacing;
        tierLayout.childAlignment = TextAnchor.MiddleLeft;
        tierLayout.childControlWidth = false;
        tierLayout.childControlHeight = false;
        tierLayout.childForceExpandWidth = false;
        tierLayout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = Undo.AddComponent<ContentSizeFitter>(generatedRoot.gameObject);
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform linesRoot = CreateRect(lineRootName, generatedRoot);
        LayoutElement lineLayout = Undo.AddComponent<LayoutElement>(linesRoot.gameObject);
        lineLayout.ignoreLayout = true;
        linesRoot.SetAsFirstSibling();

        Dictionary<ItemRecipeData, RecipeTreeSlotUI> slots = new();
        foreach (IGrouping<int, RecipeInfo> tierGroup in infos.Values.GroupBy(x => x.tier).OrderBy(x => x.Key))
        {
            RecipeTreeTierUI tier = InstantiatePrefab(tierPrefab, generatedRoot);
            tier.name = $"Tier {tierGroup.Key}";

            foreach (RecipeInfo info in tierGroup.OrderBy(x => x.data.name))
            {
                RecipeTreeSlotUI slot = InstantiatePrefab(slotPrefab, tier.SlotRoot);
                slot.name = info.data.name;
                slot.ClearLineReferences();
                slot.SetSlot(info.data, recipeManager);
                slots[info.data] = slot;
                EditorUtility.SetDirty(slot);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        foreach (RecipeInfo result in infos.Values)
        {
            if (!slots.TryGetValue(result.data, out RecipeTreeSlotUI resultSlot))
                continue;

            foreach (IGrouping<ItemRecipeData, ItemRecipeData> group in result.materials.Where(x => x != null).GroupBy(x => x))
            {
                if (!slots.TryGetValue(group.Key, out RecipeTreeSlotUI materialSlot))
                {
                    Debug.LogWarning($"[RecipeTreeBuilder] '{result.data.name}'의 재료 '{group.Key.name}' 슬롯이 목록에 없습니다.", result.data);
                    continue;
                }

                RecipeTreeLineUI line = InstantiatePrefab(linePrefab, linesRoot);
                line.name = $"{group.Key.name} -> {result.data.name} x{group.Count()}";
                line.Setup(materialSlot.transform as RectTransform, resultSlot.transform as RectTransform, group.Count());
                materialSlot.AddOutgoingLine(line);
                resultSlot.AddIncomingLine(line);
                EditorUtility.SetDirty(materialSlot);
                EditorUtility.SetDirty(resultSlot);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = generatedRoot.gameObject;
        EditorSceneManagerShim.MarkSceneDirty(contentRoot.gameObject);
        Debug.Log($"[RecipeTreeBuilder] {slots.Count}개 슬롯과 {linesRoot.childCount}개 연결선을 생성했습니다.", generatedRoot);
    }

    private bool TryReadRecipe(ItemRecipeData data, out RecipeInfo info)
    {
        SerializedObject serialized = new(data);
        SerializedProperty tier = FindProperty(serialized, tierFieldName);
        SerializedProperty materials = FindProperty(serialized, materialsFieldName);

        if (tier == null || tier.propertyType != SerializedPropertyType.Integer)
        {
            Debug.LogError($"[RecipeTreeBuilder] '{data.name}'에서 정수 필드 '{tierFieldName}'을 찾지 못했습니다.", data);
            info = null;
            return false;
        }

        if (materials == null || !materials.isArray)
        {
            Debug.LogError($"[RecipeTreeBuilder] '{data.name}'에서 배열 필드 '{materialsFieldName}'을 찾지 못했습니다.", data);
            info = null;
            return false;
        }

        List<ItemRecipeData> materialList = new();
        for (int i = 0; i < materials.arraySize; i++)
        {
            SerializedProperty element = materials.GetArrayElementAtIndex(i);
            ItemRecipeData material = ExtractRecipeReference(element);
            if (material != null)
                materialList.Add(material);
        }

        info = new RecipeInfo(data, tier.intValue, materialList);
        return true;
    }

    private static SerializedProperty FindProperty(SerializedObject obj, string pathOrName)
    {
        SerializedProperty direct = obj.FindProperty(pathOrName);
        if (direct != null)
            return direct;

        SerializedProperty iterator = obj.GetIterator();
        while (iterator.NextVisible(true))
        {
            if (iterator.name.Equals(pathOrName, StringComparison.OrdinalIgnoreCase))
                return iterator.Copy();
        }
        return null;
    }

    private static ItemRecipeData ExtractRecipeReference(SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.ObjectReference)
            return property.objectReferenceValue as ItemRecipeData;

        SerializedProperty copy = property.Copy();
        int endDepth = property.depth;
        bool enterChildren = true;
        while (copy.NextVisible(enterChildren) && copy.depth > endDepth)
        {
            enterChildren = false;
            if (copy.propertyType == SerializedPropertyType.ObjectReference &&
                copy.objectReferenceValue is ItemRecipeData recipe)
                return recipe;
        }
        return null;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create Recipe Tree Object");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static T InstantiatePrefab<T>(T prefab, Transform parent) where T : Component
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, parent) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(prefab.gameObject, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Create Recipe Tree Object");
        return instance.GetComponent<T>();
    }

    private sealed class RecipeInfo
    {
        public readonly ItemRecipeData data;
        public readonly int tier;
        public readonly List<ItemRecipeData> materials;

        public RecipeInfo(ItemRecipeData data, int tier, List<ItemRecipeData> materials)
        {
            this.data = data;
            this.tier = tier;
            this.materials = materials;
        }
    }

    private static class EditorSceneManagerShim
    {
        public static void MarkSceneDirty(GameObject target)
        {
            if (target.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.scene);
        }
    }
}
#endif
