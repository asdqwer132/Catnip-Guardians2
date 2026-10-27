using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class MergeRecipeGraphWindow : EditorWindow
{
    [SerializeField] private MergeRecipeGraphAsset graph;
    [SerializeField] private MergegameManager manager;
    private MergeRecipeGraphCanvas canvas;
    private ScrollView inspector;
    private Label status;
    private MergeGraphItemNode selected;
    private ItemData inspectedData;
    private ObjectField graphField, bookField;

    [MenuItem("Tools/Item/Merge/Recipe Book Graph")]
    public static void Open() => GetWindow<MergeRecipeGraphWindow>("Merge Recipe Book Graph");
    public static void Open(MergeRecipeGraphAsset asset)
    {
        var window = GetWindow<MergeRecipeGraphWindow>("Merge Recipe Book Graph");
        window.graph = asset; window.selected = null;
        if (window.graphField != null) window.graphField.SetValueWithoutNotify(asset);
        window.Rebuild();
    }
    public static void Open(MergeRecipeBook book)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MergeRecipeGraphAsset"))
        {
            var existing = AssetDatabase.LoadAssetAtPath<MergeRecipeGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (existing != null && existing.recipeBook == book) { Open(existing); return; }
        }
        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(book)).Replace('\\', '/');
        var graph = CreateInstance<MergeRecipeGraphAsset>();
        AssetDatabase.CreateAsset(graph, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + book.name + " Graph.asset"));
        MergeRecipeGraphEditing.ImportBook(graph, book); MergeRecipeGraphEditing.Arrange(graph); MergeRecipeGraphEditing.Save(graph);
        Open(graph);
    }
    private void OnEnable() => Undo.undoRedoPerformed += Rebuild;
    private void OnDisable() => Undo.undoRedoPerformed -= Rebuild;
    public void CreateGUI()
    {
        minSize = new Vector2(850, 520); rootVisualElement.Clear();
        var toolbar = new Toolbar();
        graphField = new ObjectField { objectType = typeof(MergeRecipeGraphAsset), allowSceneObjects = false, value = graph };
        graphField.style.width = 230;
        graphField.RegisterValueChangedCallback(evt => { graph = evt.newValue as MergeRecipeGraphAsset; selected = null; Rebuild(); });
        toolbar.Add(graphField); toolbar.Add(new ToolbarButton(NewGraph) { text = "새 그래프" });
        toolbar.Add(new ToolbarButton(() => CreateItem(canvas.Center)) { text = "+ 머지 아이템" });
        toolbar.Add(new ToolbarButton(() => ImportAssets(Selection.objects)) { text = "선택 에셋 가져오기" });
        toolbar.Add(new ToolbarButton(() => Run(() => MergeRecipeGraphEditing.Save(graph))) { text = "그래프 저장" });
        toolbar.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.Arrange(graph); Rebuild(); canvas.FrameAll(); })) { text = "자동 정렬" });
        toolbar.Add(new ToolbarButton(() => canvas.FrameAll()) { text = "전체 보기" });
        rootVisualElement.Add(toolbar);
        var output = new Toolbar();
        bookField = new ObjectField("레시피북") { objectType = typeof(MergeRecipeBook), allowSceneObjects = false, value = graph != null ? graph.recipeBook : null };
        bookField.style.width = 370;
        bookField.RegisterValueChangedCallback(evt => Run(() =>
        {
            Undo.RecordObject(graph, "Set recipe book output"); graph.recipeBook = evt.newValue as MergeRecipeBook; EditorUtility.SetDirty(graph);
        }));
        output.Add(bookField);
        output.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.ImportBook(graph, graph.recipeBook); Rebuild(); canvas.FrameAll(); })) { text = "북에서 불러오기" });
        output.Add(new ToolbarButton(() => Run(() =>
        {
            var recipes = MergeRecipeGraphEditing.ExportRecipes(graph); Rebuild(); SetStatus(recipes.Count + "개 레시피를 추출했습니다.");
        })) { text = "1. 레시피 추출" });
        output.Add(new ToolbarButton(() => Run(() => { var book = MergeRecipeGraphEditing.Export(graph); Rebuild(); SetStatus("레시피북 생성 / 갱신 완료: " + book.name); })) { text = "2. 티어별 레시피북 생성" });
        rootVisualElement.Add(output);
        var targets = new Toolbar();
        var targetField = new ObjectField("머지 매니저") { objectType = typeof(MergegameManager), allowSceneObjects = true, value = manager };
        targetField.style.width = 370; targetField.RegisterValueChangedCallback(evt => manager = evt.newValue as MergegameManager);
        targets.Add(targetField);
        targets.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.Import(graph, manager); Rebuild(); canvas.FrameAll(); })) { text = "매니저 북 불러오기" });
        targets.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.Apply(graph, manager); Rebuild(); SetStatus("매니저에 레시피북을 연결했습니다."); })) { text = "생성 후 매니저 연결" });
        targets.Add(new ToolbarButton(() => Run(() =>
        {
            var errors = MergeRecipeGraphEditing.Validate(graph);
            SetStatus(errors.Count == 0 ? "연결 검증 통과" : string.Join("\n", errors));
        })) { text = "검증" });
        rootVisualElement.Add(targets);
        var body = new VisualElement(); body.style.flexDirection = FlexDirection.Row; body.style.flexGrow = 1;
        canvas = new MergeRecipeGraphCanvas(Select, CreateItem, ImportAssets, SetStatus); body.Add(canvas);
        inspector = new ScrollView(); inspector.style.width = 300; inspector.style.minWidth = 250;
        inspector.style.paddingLeft = inspector.style.paddingRight = 8; body.Add(inspector);
        rootVisualElement.Add(body);
        status = new Label(); status.style.whiteSpace = WhiteSpace.Normal; status.style.paddingLeft = 8; status.style.paddingTop = status.style.paddingBottom = 4;
        rootVisualElement.Add(status); Rebuild();
        SetStatus("머지 아이템 출력 → 결과 아이템의 N개 입력 연결 · 우클릭 해제 · 가운데 버튼 이동 · 휠 확대 / 축소");
    }
    private void NewGraph()
    {
        string path = EditorUtility.SaveFilePanelInProject("머지 그래프 생성", "MergeRecipeGraph", "asset", "그래프 저장 위치");
        if (string.IsNullOrEmpty(path)) return;
        graph = CreateInstance<MergeRecipeGraphAsset>(); AssetDatabase.CreateAsset(graph, path);
        graphField.SetValueWithoutNotify(graph); selected = null; Rebuild();
    }
    private void Run(Action action)
    {
        if (graph == null) { SetStatus("그래프를 먼저 생성 / 연결하세요."); return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) { SetStatus("플레이를 종료한 뒤 편집하세요."); return; }
        try { action(); } catch (Exception exception) { SetStatus(exception.Message); }
    }
    private void CreateItem(Vector2 position) => Run(() =>
    {
        while (graph.items.Exists(node => Vector2.Distance(node.position, position) < 24)) position += new Vector2(36, 36);
        var item = MergeRecipeGraphEditing.CreateItem(graph, position);
        selected = graph.items.Find(node => node.item == item); Rebuild();
    });
    private void ImportAssets(Object[] assets) => Run(() =>
    {
        bool empty = graph.items.Count == 0;
        var recipes = new List<ItemRecipeData>(); int count = 0;
        foreach (var asset in assets)
        {
            if (asset is MergeItem item) { MergeRecipeGraphEditing.AddItem(graph, item, canvas.Center + new Vector2(0, count++ * 40)); }
            else if (asset is ItemRecipeData recipe) { recipes.Add(recipe); count++; }
            else if (asset is MergeRecipeBook book) { MergeRecipeGraphEditing.ImportBook(graph, book); count++; }
        }
        if (recipes.Count > 0) MergeRecipeGraphEditing.ImportRecipes(graph, recipes);
        if (empty) MergeRecipeGraphEditing.Arrange(graph);
        Rebuild(); if (count > 0) canvas.FrameAll();
        SetStatus(count > 0 ? "연결을 불러왔습니다." : "MergeItem / 레시피 / 레시피북을 선택하거나 드래그하세요.");
    });
    private void Rebuild()
    {
        if (canvas == null) return;
        if (selected != null) selected = graph != null ? graph.items.Find(node => node.id == selected.id) : null;
        canvas.SetGraph(graph, selected);
        if (bookField != null) bookField.SetValueWithoutNotify(graph != null ? graph.recipeBook : null);
        DrawInspector();
    }
    private void Select(MergeGraphItemNode asset) { selected = asset; DrawInspector(); }
    private void DrawInspector()
    {
        if (inspector == null) return;
        inspector.Clear(); inspectedData = null;
        Object target = selected != null ? selected.item : graph;
        if (target == null) return;
        inspector.Add(new Label(target.name) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10, marginBottom = 10 } });
        if (target is MergeRecipeGraphAsset)
        {
            var serialized = new SerializedObject(graph);
            foreach (string field in new[] { "tierColors", "colorMinTier", "colorMaxTier" })
                inspector.Add(new PropertyField(serialized.FindProperty(field)));
            inspector.Bind(serialized);
        }
        else
        {
            inspector.Add(new InspectorElement(target));
            if (target is MergeItem item)
            {
                inspectedData = item.item;
                if (item.item != null)
                {
                    var data = new Foldout { text = "ItemData / 아이콘 / 효과", value = false };
                    data.Add(new InspectorElement(item.item)); inspector.Add(data);
                }
                var tier = new IntegerField("노드 티어") { value = selected.tier };
                tier.RegisterValueChangedCallback(evt =>
                {
                    MergeRecipeGraphEditing.SetTier(graph, selected, evt.newValue);
                    tier.SetValueWithoutNotify(selected.tier); canvas.RefreshPreviews();
                }); inspector.Add(tier);
                if (selected.generatedRecipe != null)
                {
                    var field = new ObjectField("추출된 레시피") { objectType = typeof(ItemRecipeData), value = selected.generatedRecipe, allowSceneObjects = false };
                    field.SetEnabled(false); inspector.Add(field);
                }
            }
        }
        if (selected != null) inspector.Add(new Button(() => EditorGUIUtility.PingObject(selected.item)) { text = "Project에서 보기" });
    }
    private void OnInspectorUpdate()
    {
        if (canvas == null) return;
        canvas.RefreshPreviews();
        if (selected != null && selected.item != null && selected.item.item != inspectedData) DrawInspector();
    }
    private void SetStatus(string text) { if (status != null) status.text = text; }
}

[CustomEditor(typeof(MergeRecipeGraphAsset))]
public sealed class MergeRecipeGraphAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("레시피북 그래프 열기", GUILayout.Height(30))) MergeRecipeGraphWindow.Open((MergeRecipeGraphAsset)target);
        serializedObject.Update();
        foreach (string field in new[] { "recipeBook", "tierColors", "colorMinTier", "colorMaxTier" }) EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.LabelField("머지 아이템 " + ((MergeRecipeGraphAsset)target).items.Count);
    }
}
[CustomEditor(typeof(MergeRecipeBook))]
public sealed class MergeRecipeBookEditor : Editor
{
    public override void OnInspectorGUI()
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || !AssetDatabase.Contains(target)))
            if (GUILayout.Button("그래프로 편집", GUILayout.Height(30))) MergeRecipeGraphWindow.Open((MergeRecipeBook)target);
        DrawDefaultInspector();
    }
}
