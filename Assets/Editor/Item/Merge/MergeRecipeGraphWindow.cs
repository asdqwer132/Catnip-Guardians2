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
    private FloatField horizontalGap, verticalGap;
    private EnumField direction;
    private Toggle fromInputs;

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
        toolbar.Add(new ToolbarButton(() => ImportAssets(Selection.objects)) { text = "선택 에셋 가져오기", tooltip = "ItemData 여러 개를 선택하면 각각 MergeItem으로 만들어 추가합니다." });
        toolbar.Add(new ToolbarButton(() => Run(() => MergeRecipeGraphEditing.Save(graph))) { text = "그래프 저장" });
        toolbar.Add(new ToolbarButton(() => canvas.FrameAll()) { text = "전체 보기" });
        rootVisualElement.Add(toolbar);
        var layout = new Toolbar();
        direction = new EnumField("배치 방향", graph != null ? graph.arrangeDirection : MergeGraphArrangeDirection.Vertical);
        direction.style.width = 280;
        direction.RegisterValueChangedCallback(evt => Run(() =>
        {
            Undo.RecordObject(graph, "Set merge arrangement direction");
            graph.arrangeDirection = (MergeGraphArrangeDirection)evt.newValue; EditorUtility.SetDirty(graph);
        })); layout.Add(direction);
        fromInputs = new Toggle("시작 재료 위치 기준") { value = graph == null || graph.useInputPositions };
        fromInputs.RegisterValueChangedCallback(evt => Run(() =>
        {
            Undo.RecordObject(graph, "Choose merge layout anchors"); graph.useInputPositions = evt.newValue; EditorUtility.SetDirty(graph);
        })); layout.Add(fromInputs);
        horizontalGap = AddGapField(layout, "가로 간격", true);
        verticalGap = AddGapField(layout, "세로 간격", false);
        layout.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.Arrange(graph); Rebuild(); canvas.FrameAll(); }))
        { text = "자동 정렬", tooltip = "시작 재료 위치 기준: 입력 없는 노드는 고정하고 결과를 배치합니다. 해제하면 깊이/행 설정을 사용합니다." });
        rootVisualElement.Add(layout);
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
    private FloatField AddGapField(Toolbar toolbar, string label, bool horizontal)
    {
        var field = new FloatField(label) { tooltip = "노드 테두리 사이의 빈 간격입니다. 설정은 그래프별로 저장됩니다." };
        field.style.width = 170; field.labelElement.style.minWidth = 65;
        field.RegisterValueChangedCallback(evt => Run(() =>
        {
            float value = float.IsNaN(evt.newValue) || float.IsInfinity(evt.newValue) ? 0 : Mathf.Max(0, evt.newValue);
            Undo.RecordObject(graph, "Change merge node spacing");
            if (horizontal) graph.nodeSpacing.x = value; else graph.nodeSpacing.y = value;
            field.SetValueWithoutNotify(value); EditorUtility.SetDirty(graph);
        }));
        toolbar.Add(field); return field;
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
        if (empty && data.Count == 0) MergeRecipeGraphEditing.Arrange(graph);
        Rebuild(); if (count > 0) canvas.FrameAll();
        SetStatus(data.Count > 0 ? $"ItemData에서 {added}개 노드를 추가했습니다. 이미 있는 항목은 유지합니다." :
            count > 0 ? "연결을 불러왔습니다." : "ItemData / MergeItem / 레시피 / 레시피북을 선택하거나 드래그하세요.");
    });
    private void Rebuild()
    {
        if (canvas == null) return;
        if (selected != null) selected = graph != null ? graph.items.Find(node => node.id == selected.id) : null;
        canvas.SetGraph(graph, selected);
        direction?.SetValueWithoutNotify(graph != null ? graph.arrangeDirection : MergeGraphArrangeDirection.Vertical);
        fromInputs?.SetValueWithoutNotify(graph == null || graph.useInputPositions);
        selected = canvas.PrimarySelection;
        horizontalGap?.SetValueWithoutNotify(graph != null ? graph.nodeSpacing.x : 140);
        verticalGap?.SetValueWithoutNotify(graph != null ? graph.nodeSpacing.y : 45);
        horizontalGap?.SetEnabled(graph != null); verticalGap?.SetEnabled(graph != null);
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
            foreach (string field in new[] { "arrangeDirection", "useInputPositions", "keepBranchRows", "nodeSpacing" })
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
                var useDepth = new Toggle("시작 깊이 지정") { value = selected.overrideStartDepth };
                var depth = new IntegerField("시작 깊이 (0부터)") { value = selected.startDepth, isDelayed = true };
                depth.tooltip = "자동 배치의 최소 시작 깊이. 연결된 다음 노드도 이 깊이 이후에 배치됩니다.";
                depth.SetEnabled(selected.overrideStartDepth);
                useDepth.RegisterValueChangedCallback(evt => Run(() =>
                {
                    Undo.RecordObject(graph, "Override merge starting depth");
                    EditSelection(node => node.overrideStartDepth = evt.newValue); depth.SetEnabled(evt.newValue); EditorUtility.SetDirty(graph);
                }));
                depth.RegisterValueChangedCallback(evt => Run(() =>
                {
                    Undo.RecordObject(graph, "Set merge starting depth");
                    EditSelection(node => node.startDepth = Mathf.Max(0, evt.newValue)); depth.SetValueWithoutNotify(selected.startDepth); EditorUtility.SetDirty(graph);
                }));
                inspector.Add(useDepth); inspector.Add(depth);
                var pinRow = new Toggle("배치 행 지정 (선택 노드 전체)") { value = selected.overrideRow };
                var row = new IntegerField("행 (0부터)") { value = selected.row, isDelayed = true };
                pinRow.RegisterValueChangedCallback(evt => Run(() => EditSelection(node => node.overrideRow = evt.newValue)));
                void ApplyRow()
                {
                    int value = Mathf.Max(0, row.value);
                    EditSelection(node => { node.overrideRow = true; node.row = value; });
                    pinRow.SetValueWithoutNotify(true);
                    row.SetValueWithoutNotify(value);
                }
                row.RegisterValueChangedCallback(evt => Run(ApplyRow));
                inspector.Add(pinRow); inspector.Add(row);
                inspector.Add(new Button(() => Run(ApplyRow)) { text = "행 적용 (선택 노드 전체)",
                    tooltip = "입력값이 기존 값과 같아도 선택한 모든 노드에 행 지정과 행 번호를 적용합니다." });
                var position = new Vector2Field("Position X / Y") { value = selected.position };
                position.RegisterValueChangedCallback(evt => Run(() =>
                {
                    if (!float.IsFinite(evt.newValue.x) || !float.IsFinite(evt.newValue.y)) return;
                    Undo.RecordObject(graph, "Set merge node position");
                    selected.position = evt.newValue; EditorUtility.SetDirty(graph); Rebuild();
                })); inspector.Add(position);
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
        if (graph != null)
        {
            horizontalGap?.SetValueWithoutNotify(graph.nodeSpacing.x);
            verticalGap?.SetValueWithoutNotify(graph.nodeSpacing.y);
        }
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
        foreach (string field in new[] { "recipeBook", "arrangeDirection", "useInputPositions", "keepBranchRows", "nodeSpacing" }) EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
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
