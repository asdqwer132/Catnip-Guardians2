using System;
using System.Collections.Generic;
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
    private Object selected;
    private ItemData inspectedData;
    private ObjectField graphField;

    [MenuItem("Tools/Item/Merge Recipe Graph")]
    public static void Open() => GetWindow<MergeRecipeGraphWindow>("Merge Recipe Graph");

    public static void Open(MergeRecipeGraphAsset asset)
    {
        var window = GetWindow<MergeRecipeGraphWindow>("Merge Recipe Graph");
        window.graph = asset;
        if (window.graphField != null) window.graphField.SetValueWithoutNotify(asset);
        window.Rebuild();
    }

    private void OnEnable() => Undo.undoRedoPerformed += Rebuild;
    private void OnDisable() => Undo.undoRedoPerformed -= Rebuild;

    public void CreateGUI()
    {
        minSize = new Vector2(850, 500);
        var root = rootVisualElement;
        root.Clear();
        var toolbar = new Toolbar();
        graphField = new ObjectField { objectType = typeof(MergeRecipeGraphAsset), allowSceneObjects = false, value = graph };
        graphField.style.width = 240;
        graphField.RegisterValueChangedCallback(evt => { graph = evt.newValue as MergeRecipeGraphAsset; selected = null; Rebuild(); });
        toolbar.Add(graphField);
        toolbar.Add(new ToolbarButton(NewGraph) { text = "새 그래프" });
        var add = new ToolbarMenu { text = "+" };
        add.menu.AppendAction("머지 아이템 생성", _ => CreateItem(canvas.Center));
        add.menu.AppendAction("레시피 생성", _ => CreateRecipe(canvas.Center));
        add.menu.AppendAction("Project 선택 에셋 가져오기", _ => ImportAssets(Selection.objects));
        toolbar.Add(add);
        toolbar.Add(new ToolbarButton(() => ImportAssets(Selection.objects)) { text = "선택 에셋 불러오기" });
        toolbar.Add(new ToolbarButton(() => Run(() => MergeRecipeGraphEditing.Save(graph))) { text = "저장" });
        toolbar.Add(new ToolbarButton(() => { if (graph != null) { MergeRecipeGraphEditing.Arrange(graph); Rebuild(); canvas.FrameAll(); } }) { text = "자동 정렬" });
        toolbar.Add(new ToolbarButton(() => canvas.FrameAll()) { text = "전체 보기" });
        root.Add(toolbar);

        var targets = new Toolbar();
        var managerField = new ObjectField("머지 매니저") { objectType = typeof(MergegameManager), allowSceneObjects = true, value = manager };
        managerField.style.width = 360;
        managerField.RegisterValueChangedCallback(evt => manager = evt.newValue as MergegameManager);
        targets.Add(managerField);
        targets.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.Import(graph, manager); Rebuild(); canvas.FrameAll(); })) { text = "매니저에서 불러오기" });
        targets.Add(new ToolbarButton(() => Run(() => { MergeRecipeGraphEditing.Apply(graph, manager); SetStatus("매니저의 레시피 / 아이템 설정에 적용했습니다. 재고와 UI는 유지됩니다."); })) { text = "매니저에 적용" });
        targets.Add(new ToolbarButton(() => Run(() =>
        {
            var errors = MergeRecipeGraphEditing.Validate(graph);
            SetStatus(errors.Count == 0 ? "레시피 검증 통과" : string.Join("\n", errors));
        })) { text = "검증" });
        root.Add(targets);

        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexGrow = 1;
        canvas = new MergeRecipeGraphCanvas(Select, CreateItem, CreateRecipe, ImportAssets, SetStatus);
        body.Add(canvas);
        inspector = new ScrollView();
        inspector.style.width = 300;
        inspector.style.minWidth = 250;
        inspector.style.paddingLeft = inspector.style.paddingRight = 8;
        body.Add(inspector);
        root.Add(body);
        status = new Label();
        status.style.whiteSpace = WhiteSpace.Normal;
        status.style.paddingLeft = 8;
        status.style.paddingTop = status.style.paddingBottom = 4;
        root.Add(status);
        Rebuild();
        SetStatus("포트 드래그: 연결  ·  포트 우클릭: 해제  ·  휠: 확대/축소  ·  가운데 버튼: 이동");
    }

    private void NewGraph()
    {
        string path = EditorUtility.SaveFilePanelInProject("머지 그래프 생성", "MergeRecipeGraph", "asset", "그래프를 저장할 위치를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;
        graph = CreateInstance<MergeRecipeGraphAsset>();
        AssetDatabase.CreateAsset(graph, path);
        graphField.SetValueWithoutNotify(graph);
        selected = null;
        Rebuild();
    }

    private void Run(Action action)
    {
        if (graph == null) { SetStatus("먼저 새 그래프를 만들거나 그래프 에셋을 연결하세요."); return; }
        if (EditorApplication.isPlaying) { SetStatus("플레이를 종료한 뒤 편집하세요."); return; }
        try { action(); }
        catch (Exception exception) { SetStatus(exception.Message); }
    }

    private void CreateItem(Vector2 position) => Run(() =>
    {
        var item = MergeRecipeGraphEditing.CreateItem(graph, FreePosition(position));
        selected = item;
        Rebuild();
    });

    private void CreateRecipe(Vector2 position) => Run(() =>
    {
        var recipe = MergeRecipeGraphEditing.CreateRecipe(graph, FreePosition(position));
        selected = recipe;
        Rebuild();
    });

    private void ImportAssets(Object[] assets) => Run(() =>
    {
        bool wasEmpty = graph.items.Count == 0 && graph.recipes.Count == 0;
        var recipes = new List<ItemRecipeData>();
        foreach (var asset in assets)
            if (asset is ItemRecipeData recipe && !MergeRecipeGraphEditing.TryInputs(recipe, out _, out _))
                throw new InvalidOperationException(recipe.name + ": 두 재료를 사용하는 레시피만 불러올 수 있습니다.");
        int count = 0;
        foreach (var asset in assets)
        {
            if (asset is MergeItem item) { MergeRecipeGraphEditing.AddItem(graph, item, canvas.Center + new Vector2(0, count++ * 40)); }
            else if (asset is ItemRecipeData recipe) { recipes.Add(recipe); count++; }
        }
        if (recipes.Count > 0) MergeRecipeGraphEditing.ImportRecipes(graph, recipes);
        if (wasEmpty && recipes.Count > 0) MergeRecipeGraphEditing.Arrange(graph);
        Rebuild();
        if (count > 0) canvas.FrameAll();
        SetStatus(count > 0 ? "기존 에셋과 레시피 연결을 불러왔습니다." : "Project에서 MergeItem / ItemRecipeData를 선택하거나 그래프에 드래그하세요.");
    });

    private Vector2 FreePosition(Vector2 position)
    {
        for (int i = 0; i <= graph.items.Count + graph.recipes.Count; i++)
        {
            bool occupied = graph.items.Exists(node => Vector2.Distance(node.position, position) < 24) ||
                graph.recipes.Exists(node => Vector2.Distance(node.position, position) < 24);
            if (!occupied) break;
            position += new Vector2(36, 36);
        }
        return position;
    }

    private void Rebuild()
    {
        if (canvas == null) return;
        canvas.SetGraph(graph, selected);
        DrawInspector();
    }

    private void Select(Object asset)
    {
        selected = asset;
        DrawInspector();
    }

    private void DrawInspector()
    {
        if (inspector == null) return;
        inspector.Clear();
        inspectedData = null;
        Object target = selected != null ? selected : graph;
        if (target == null) return;
        inspector.Add(new Label(target.name) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10, marginBottom = 10 } });
        if (target is MergeRecipeGraphAsset)
        {
            var serialized = new SerializedObject(graph);
            inspector.Add(new PropertyField(serialized.FindProperty("defaultSeries"), "새 아이템 시리즈"));
            inspector.Add(new PropertyField(serialized.FindProperty("defaultPrefab"), "새 아이템 프리팹 템플릿"));
            inspector.Bind(serialized);
        }
        else
        {
            inspector.Add(new InspectorElement(target));
            if (target is MergeItem item && item.item != null)
            {
                inspectedData = item.item;
                var data = new Foldout { text = "ItemData · 아이콘 / 시리즈 / 효과", value = false };
                data.Add(new InspectorElement(item.item));
                inspector.Add(data);
            }
        }
        if (selected != null)
            inspector.Add(new Button(() => EditorGUIUtility.PingObject(selected)) { text = "Project에서 보기" });
    }

    private void OnInspectorUpdate()
    {
        if (canvas == null) return;
        canvas.RefreshPreviews();
        if (selected is MergeItem item && item.item != inspectedData) DrawInspector();
    }

    private void SetStatus(string message)
    {
        if (status != null) status.text = message;
    }
}

[CustomEditor(typeof(MergeRecipeGraphAsset))]
public sealed class MergeRecipeGraphAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("노드 에디터 열기", GUILayout.Height(30))) MergeRecipeGraphWindow.Open((MergeRecipeGraphAsset)target);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultSeries"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultPrefab"));
        serializedObject.ApplyModifiedProperties();
        var graph = (MergeRecipeGraphAsset)target;
        EditorGUILayout.LabelField($"아이템 {graph.items.Count} · 레시피 {graph.recipes.Count}");
    }
}
