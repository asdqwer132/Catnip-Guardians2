using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillTreeGraphWindow : EditorWindow
{
    [SerializeField] private SkillTreeLayoutData layout;
    private SkillNodeData addNode;
    private SkillTreeNodePlacer placer;
    private Canvas graph;
    [MenuItem("Tools/Skills/Tree Graph")]
    public static void Open() => GetWindow<SkillTreeGraphWindow>("Skill Tree Graph");
    public static void Open(SkillTreeLayoutData data)
    { var window = GetWindow<SkillTreeGraphWindow>("Skill Tree Graph"); window.layout = data; window.Rebuild(); }
    private void OnFocus() { if (rootVisualElement != null) Rebuild(); }
    private void OnEnable() { Undo.undoRedoPerformed += Rebuild; Rebuild(); }
    private void OnDisable() => Undo.undoRedoPerformed -= Rebuild;
    private void Rebuild()
    {
        rootVisualElement.Clear();
        var bar = new Toolbar();
        var field = new ObjectField("Layout") { objectType = typeof(SkillTreeLayoutData), value = layout };
        field.RegisterValueChangedCallback(evt => { layout = evt.newValue as SkillTreeLayoutData; Rebuild(); }); bar.Add(field);
        bar.Add(new ToolbarButton(() =>
        {
            string path = EditorUtility.SaveFilePanelInProject("Create layout", "SkillTreeLayout", "asset", "");
            if (string.IsNullOrEmpty(path)) return;
            layout = CreateInstance<SkillTreeLayoutData>(); AssetDatabase.CreateAsset(layout, path); Rebuild();
        }) { text = "New Layout" });
        bar.Add(new ToolbarButton(() =>
        {
            if (layout == null) return;
            string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(layout)).Replace('\\', '/');
            var existing = layout.nodeDatas.FirstOrDefault(value => value != null);
            if (existing != null) folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(existing)).Replace('\\', '/');
            else if (AssetDatabase.IsValidFolder(folder + "/Node")) folder += "/Node";
            int next = 1;
            foreach (var value in layout.nodeDatas)
                if (value != null)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(value.name, @"^SkillNodeData\s+(\d+)$");
                    if (match.Success) next = System.Math.Max(next, int.Parse(match.Groups[1].Value) + 1);
                }
            string path = EditorUtility.SaveFilePanelInProject("Create node", "SkillNodeData " + next, "asset", "", folder);
            if (string.IsNullOrEmpty(path)) return;
            var node = CreateInstance<SkillNodeData>(); node.skillMap = layout.skillMap; node.dataId = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(node, path); SkillTreeGraphEditing.Add(layout, node, new Vector2(layout.nodeDatas.Count * 260, 0)); Rebuild();
        }) { text = "+ Node" });
        bar.Add(new ToolbarButton(() => { AssetDatabase.SaveAssets(); graph?.FrameAll(); }) { text = "Save / Frame" });
        rootVisualElement.Add(bar);
        var inputs = new Toolbar();
        if (layout != null)
        {
            var mapField = new ObjectField("Map") { objectType = typeof(SkillMapData), value = layout.skillMap };
            mapField.RegisterValueChangedCallback(evt => { Undo.RecordObject(layout, "Set graph map"); layout.skillMap = evt.newValue as SkillMapData; EditorUtility.SetDirty(layout); });
            inputs.Add(mapField);
        }
        var nodeField = new ObjectField("Existing Node") { objectType = typeof(SkillNodeData), value = addNode };
        nodeField.RegisterValueChangedCallback(evt => addNode = evt.newValue as SkillNodeData); inputs.Add(nodeField);
        inputs.Add(new ToolbarButton(() => { if (layout == null) return; SkillTreeGraphEditing.Add(layout, addNode, new Vector2(layout.nodeDatas.Count * 260, 0)); Rebuild(); }) { text = "Add" });
        var placerField = new ObjectField("Scene Placer") { objectType = typeof(SkillTreeNodePlacer), allowSceneObjects = true, value = placer };
        placerField.RegisterValueChangedCallback(evt => placer = evt.newValue as SkillTreeNodePlacer); inputs.Add(placerField);
        inputs.Add(new ToolbarButton(() =>
        {
            if (placer == null || layout == null) return;
            Undo.RecordObject(placer, "Assign skill layout"); placer.layout = layout; EditorUtility.SetDirty(placer); placer.Generate();
        }) { text = "Apply To Scene" });
        rootVisualElement.Add(inputs);
        rootVisualElement.Add(new Label("Drag node assets here. Connect a parent's Output to a child's Requires port. Existing unlock rules still apply (any connected prerequisite)."));
        graph = new Canvas(layout); graph.style.flexGrow = 1; rootVisualElement.Add(graph);
    }
    private sealed class Card : Node
    {
        public SkillNodeData data;
        public Port input, output;
        public Card(SkillNodeData data)
        {
            this.data = data; title = data.name;
            input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(SkillNodeData)); input.portName = "Requires";
            output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(SkillNodeData)); output.portName = "Output";
            inputContainer.Add(input); outputContainer.Add(output);
            var button = new Button(() => { Selection.activeObject = data; EditorGUIUtility.PingObject(data); }) { text = "Inspect Node / Rewards" };
            extensionContainer.Add(button); RefreshExpandedState(); RefreshPorts();
        }
    }
    private sealed class Canvas : GraphView
    {
        private readonly SkillTreeLayoutData data;
        public Canvas(SkillTreeLayoutData data)
        {
            this.data = data; SetupZoom(.15f, 2f);
            this.AddManipulator(new ContentDragger()); this.AddManipulator(new SelectionDragger()); this.AddManipulator(new RectangleSelector());
            Insert(0, new GridBackground());
            if (data == null) return;
            var cards = new Dictionary<SkillNodeData, Card>();
            foreach (var node in data.nodeDatas)
            {
                if (node == null || cards.ContainsKey(node)) continue;
                var card = new Card(node); var saved = data.nodeLayout.Find(value => value != null && value.node == node);
                var position = saved != null ? saved.position : Vector2.zero;
                card.SetPosition(new Rect(position.x, -position.y, 220, 100)); AddElement(card); cards.Add(node, card);
            }
            foreach (var pair in cards)
                foreach (var parent in pair.Key.requiredSkills ?? new List<SkillNodeData>())
                    if (parent != null && cards.TryGetValue(parent, out var source)) AddElement(source.output.ConnectTo(pair.Value.input));
            graphViewChanged = Changed;
            RegisterCallback<DragUpdatedEvent>(evt => { if (DragAndDrop.objectReferences.Any(value => value is SkillNodeData)) DragAndDrop.visualMode = DragAndDropVisualMode.Copy; });
            RegisterCallback<DragPerformEvent>(evt =>
            {
                var point = contentViewContainer.WorldToLocal(evt.mousePosition);
                int index = 0;
                foreach (var value in DragAndDrop.objectReferences)
                    if (value is SkillNodeData node)
                    {
                        SkillTreeGraphEditing.Add(data, node, new Vector2(point.x + index % 5 * 260, -point.y - index / 5 * 150));
                        index++;
                    }
                DragAndDrop.AcceptDrag(); GetWindow<SkillTreeGraphWindow>().Rebuild();
            });
        }
        public override List<Port> GetCompatiblePorts(Port start, NodeAdapter adapter)
            => ports.Where(port => port.direction != start.direction && port.node != start.node).ToList();
        private GraphViewChange Changed(GraphViewChange change)
        {
            if (change.movedElements != null)
                foreach (var element in change.movedElements)
                    if (element is Card card) SkillTreeGraphEditing.Move(data, card.data, new Vector2(card.GetPosition().x, -card.GetPosition().y));
            if (change.elementsToRemove != null)
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge) SkillTreeGraphEditing.Disconnect(((Card)edge.output.node).data, ((Card)edge.input.node).data);
                    if (element is Card card) SkillTreeGraphEditing.Remove(data, card.data);
                }
            if (change.edgesToCreate != null)
                change.edgesToCreate.RemoveAll(edge => !SkillTreeGraphEditing.Connect(data, ((Card)edge.output.node).data, ((Card)edge.input.node).data));
            return change;
        }
    }
}

[CustomEditor(typeof(SkillTreeLayoutData))]
public sealed class SkillTreeLayoutDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Skill Tree Graph")) SkillTreeGraphWindow.Open((SkillTreeLayoutData)target);
        DrawDefaultInspector();
    }
}
