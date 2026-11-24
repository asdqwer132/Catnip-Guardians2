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
    private EnumField mode;
    private Vector2Field cellSize, cellGap;

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
        MergeRecipeGraphEditing.ImportBook(graph, book); MergeRecipeGraphEditing.Save(graph);
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
        toolbar.Add(new ToolbarButton(() => ImportAssets(Selection.objects)) { text = "선택 에셋 가져오기", tooltip = "ItemData 여러 개를 선택하면 각각 MergeItem으로 만들어 추가합니다." });
        toolbar.Add(new ToolbarButton(() => Run(() => MergeRecipeGraphEditing.Save(graph))) { text = "그래프 저장" });
        toolbar.Add(new ToolbarButton(() => canvas.FrameAll()) { text = "전체 보기" });
        rootVisualElement.Add(toolbar);
        var layout = new Toolbar();
        mode = new EnumField("배치 모드", graph != null ? graph.placementMode : MergeGraphPlacementMode.Normal);
        mode.style.width = 260;
        mode.RegisterValueChangedCallback(evt => Run(() => { MergeRecipeGraphLayout.SetMode(graph, (MergeGraphPlacementMode)evt.newValue); Rebuild(); }));
        layout.Add(mode);
        var gridOptions = new Toolbar();
        cellSize = new Vector2Field("격자 간격") { value = graph != null ? graph.gridCellSize : new Vector2(400, 360) };
        cellGap = new Vector2Field("같은 셀 노드 간격") { value = graph != null ? graph.sameCellSpacing : new Vector2(20, 20) };
        cellSize.style.width = cellGap.style.width = 270;
        void UpdateGrid()
        {
            if (!float.IsFinite(cellSize.value.x) || !float.IsFinite(cellSize.value.y) || !float.IsFinite(cellGap.value.x) || !float.IsFinite(cellGap.value.y)) return;
            Undo.RecordObject(graph, "Change merge grid spacing");
            graph.gridCellSize = Vector2.Max(Vector2.one, cellSize.value);
            graph.sameCellSpacing = Vector2.Max(Vector2.one, cellGap.value);
            MergeRecipeGraphLayout.Arrange(graph); Rebuild();
        }
        cellSize.RegisterValueChangedCallback(evt => Run(UpdateGrid)); cellGap.RegisterValueChangedCallback(evt => Run(UpdateGrid));
        gridOptions.Add(cellSize); gridOptions.Add(cellGap);
        layout.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.Arrange(graph); Rebuild(); })) { text = "격자 배치 적용" });
        rootVisualElement.Add(layout); rootVisualElement.Add(gridOptions);
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
        output.Add(new ToolbarButton(() => Run(() => { var book = MergeRecipeGraphEditing.Export(graph); Rebuild(); SetStatus("레시피북 생성 / 갱신 완료: " + book.name); })) { text = "2. 레시피북 생성" });
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
        SetStatus("Shift / Ctrl / Cmd + 클릭: 다중 선택 · 빈 곳 드래그: 영역 선택 · Ctrl / Cmd + C,V: 복사·붙여넣기 / D: 복제 · Delete: 삭제 · 가운데 버튼 이동 · 휠 확대 / 축소");
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
        var recipes = new List<ItemRecipeData>();
        var data = new List<ItemData>(); int count = 0;
        foreach (var asset in assets)
        {
            if (asset is ItemData itemData) data.Add(itemData);
            else if (asset is MergeItem item) { MergeRecipeGraphEditing.AddItem(graph, item, canvas.Center + new Vector2(0, count++ * 40)); }
            else if (asset is ItemRecipeData recipe) { recipes.Add(recipe); count++; }
            else if (asset is MergeRecipeBook book) { MergeRecipeGraphEditing.ImportBook(graph, book); count++; }
        }
        if (recipes.Count > 0) MergeRecipeGraphEditing.ImportRecipes(graph, recipes);
        int added = data.Count > 0 ? MergeRecipeGraphEditing.ImportItemData(graph, data, canvas.Center) : 0;
        count += added;
        if (graph.placementMode == MergeGraphPlacementMode.Grid) MergeRecipeGraphEditing.Arrange(graph);
        Rebuild(); if (count > 0) canvas.FrameAll();
        SetStatus(data.Count > 0 ? $"ItemData에서 {added}개 노드를 추가했습니다. 이미 있는 항목은 유지합니다." :
            count > 0 ? "연결을 불러왔습니다." : "ItemData / MergeItem / 레시피 / 레시피북을 선택하거나 드래그하세요.");
    });
    private void Rebuild()
    {
        if (canvas == null) return;
        if (selected != null) selected = graph != null ? graph.items.Find(node => node.id == selected.id) : null;
        canvas.SetGraph(graph, selected);
        mode?.SetValueWithoutNotify(graph != null ? graph.placementMode : MergeGraphPlacementMode.Normal);
        cellSize?.SetValueWithoutNotify(graph != null ? graph.gridCellSize : new Vector2(400, 360));
        cellGap?.SetValueWithoutNotify(graph != null ? graph.sameCellSpacing : new Vector2(20, 20));
        if (bookField != null) bookField.SetValueWithoutNotify(graph != null ? graph.recipeBook : null);
        DrawInspector();
    }
    private void Select(MergeGraphItemNode asset) { selected = asset; DrawInspector(); }
    private void DrawInspector()
    {
        if (inspector == null) return;
        inspector.Clear(); inspectedData = null;
        if (canvas.SelectionCount > 1) inspector.Add(new Label(canvas.SelectionCount + "개 선택 · 아래는 마지막 선택 노드"));
        Object target = selected != null ? selected.item : graph;
        if (target == null) return;
        inspector.Add(new Label(target.name) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10, marginBottom = 10 } });
        if (target is MergeRecipeGraphAsset)
        {
            var serialized = new SerializedObject(graph);
            foreach (string field in new[] { "gridCellSize", "sameCellSpacing" })
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
                if (graph.placementMode == MergeGraphPlacementMode.Grid)
                {
                    var coordinate = new Vector2IntField("격자 X / Y (선택 전체)") { value = selected.gridPosition };
                    void ApplyCoordinate()
                    {
                        EditSelection(node => node.gridPosition = coordinate.value);
                        MergeRecipeGraphLayout.Arrange(graph); Rebuild();
                    }
                    coordinate.RegisterValueChangedCallback(evt => Run(ApplyCoordinate));
                    inspector.Add(coordinate);
                    inspector.Add(new Button(() => Run(ApplyCoordinate)) { text = "좌표 적용 (선택 전체)" });
                }
                else
                {
                    var position = new Vector2Field("Position X / Y") { value = selected.position };
                    position.RegisterValueChangedCallback(evt => Run(() =>
                    {
                        if (!float.IsFinite(evt.newValue.x) || !float.IsFinite(evt.newValue.y)) return;
                        Vector2 delta = evt.newValue - selected.position;
                        EditSelection(node => node.position += delta); Rebuild();
                    })); inspector.Add(position);
                }
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
    private void EditSelection(Action<MergeGraphItemNode> edit)
    {
        Undo.RecordObject(graph, "Edit selected merge nodes");
        var nodes = canvas.SelectedNodes;
        if (nodes.Count == 0 && selected != null) nodes.Add(selected);
        foreach (var node in nodes) edit(node);
        EditorUtility.SetDirty(graph);
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
        foreach (string field in new[] { "recipeBook", "gridCellSize", "sameCellSpacing" }) EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
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
