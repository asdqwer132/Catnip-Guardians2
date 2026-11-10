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
    [SerializeField] private float spacingScale = 1f;
    [SerializeField] private float positionScale = 1f;
    [SerializeField] private float maximumZoom = 10f;
    [MenuItem("Tools/Skill/Tree Graph")]
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
        var viewOptions = new Toolbar();
        var displaySpacing = new FloatField("Spacing Multiplier") { value = spacingScale, isDelayed = true };
        displaySpacing.style.width = 280;
        displaySpacing.tooltip = "Visual spacing only; saved node coordinates remain unchanged.";
        displaySpacing.RegisterValueChangedCallback(evt => { if (float.IsFinite(evt.newValue)) spacingScale = Mathf.Clamp(evt.newValue, .1f, 20f); Rebuild(); });
        viewOptions.Add(displaySpacing);
        rootVisualElement.Add(viewOptions);
        viewOptions = new Toolbar();
        var spacing = new FloatField("Position Scale X / Y") { value = positionScale, isDelayed = true };
        spacing.style.width = 280;
        spacing.tooltip = "Multiply saved X and Y coordinates around (0, 0). 0.5 halves all distances. Undo supported.";
        spacing.RegisterValueChangedCallback(evt =>
        {
            if (float.IsFinite(evt.newValue) && evt.newValue > 0) positionScale = evt.newValue;
            else spacing.SetValueWithoutNotify(positionScale);
        });
        viewOptions.Add(spacing);
        viewOptions.Add(new ToolbarButton(() =>
        {
            if (layout == null || !float.IsFinite(positionScale) || positionScale <= 0) return;
            Undo.RecordObject(layout, "Scale skill node positions");
            foreach (var entry in layout.nodeLayout)
                if (entry != null) entry.position *= positionScale;
            layout.revision++;
            EditorUtility.SetDirty(layout);
            Rebuild();
        }) { text = "Apply Position Scale" });
        var zoomLimit = new FloatField("Maximum Zoom") { value = maximumZoom, isDelayed = true };
        zoomLimit.style.width = 250;
        zoomLimit.RegisterValueChangedCallback(evt => { if (float.IsFinite(evt.newValue)) maximumZoom = Mathf.Clamp(evt.newValue, 2f, 30f); Rebuild(); });
        viewOptions.Add(zoomLimit);
        rootVisualElement.Add(viewOptions);
        graph = new Canvas(layout, spacingScale, maximumZoom); graph.style.flexGrow = 1; rootVisualElement.Add(graph);
    }
    private sealed class Card : Node
    {
        public SkillNodeData data;
        public Port input, output;
        private readonly Vector2Field positionField;
        private readonly SkillTreeLayoutData layoutData;
        private readonly float spacingScale;
        public Card(SkillNodeData data, SkillTreeLayoutData layout, float spacingScale = 1f)
        {
            this.data = data; layoutData = layout; this.spacingScale = spacingScale; title = data.name;
            input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(SkillNodeData)); input.portName = "Requires";
            output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(SkillNodeData)); output.portName = "Output";
            inputContainer.Add(input); outputContainer.Add(output);
            var button = new Button(() => { Selection.activeObject = data; EditorGUIUtility.PingObject(data); }) { text = "Inspect Node / Rewards" };
            positionField = new Vector2Field("Position")
            {
                tooltip = "Layout coordinates: X increases right, Y increases up. Matches the scene UI placement."
            };
            positionField.style.width = 380;
            positionField.labelElement.style.minWidth = 60;
            positionField.labelElement.style.width = 60;
            style.minWidth = 410;
            positionField.RegisterValueChangedCallback(evt =>
            {
                if (!float.IsFinite(evt.newValue.x) || !float.IsFinite(evt.newValue.y))
                {
                    positionField.SetValueWithoutNotify(evt.previousValue);
                    return;
                }
                SetLayoutPosition(evt.newValue);
            });
            extensionContainer.Add(positionField);
            extensionContainer.Add(button); RefreshExpandedState(); RefreshPorts();
        }
        public void SetLayoutPosition(Vector2 position)
        {
            SkillTreeGraphEditing.Move(layoutData, data, position);
            var rect = GetPosition();
            rect.position = new Vector2(position.x, -position.y) * spacingScale;
            SetPosition(rect);
        }
        public override void SetPosition(Rect position)
        {
            base.SetPosition(position);
            positionField?.SetValueWithoutNotify(new Vector2(position.x, -position.y) / spacingScale);
        }
    }
    private sealed class Canvas : GraphView
    {
        private readonly SkillTreeLayoutData data;
        private readonly float spacingScale;
        public Canvas(SkillTreeLayoutData data) : this(data, 1f, 10f) { }
        public Canvas(SkillTreeLayoutData data, float spacingScale, float maximumZoom)
        {
            this.data = data; this.spacingScale = spacingScale; SetupZoom(.05f, maximumZoom);
            this.AddManipulator(new ContentDragger()); this.AddManipulator(new SelectionDragger()); this.AddManipulator(new RectangleSelector());
            var coordinates = new IMGUIContainer(DrawCoordinates) { pickingMode = PickingMode.Ignore };
            Insert(0, coordinates);
            coordinates.StretchToParentSize();
            viewTransformChanged += _ => coordinates.MarkDirtyRepaint();
            RegisterCallback<GeometryChangedEvent>(_ => coordinates.MarkDirtyRepaint());
            if (data == null) return;
            var cards = new Dictionary<SkillNodeData, Card>();
            foreach (var node in data.nodeDatas)
            {
                if (node == null || cards.ContainsKey(node)) continue;
                var card = new Card(node, data, spacingScale); var saved = data.nodeLayout.Find(value => value != null && value.node == node);
                var position = saved != null ? saved.position : Vector2.zero;
                card.SetPosition(new Rect(position.x * spacingScale, -position.y * spacingScale, 410, 100)); AddElement(card); cards.Add(node, card);
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
                        SkillTreeGraphEditing.Add(data, node, new Vector2(point.x + index % 5 * 440, -point.y - index / 5 * 180) / spacingScale);
                        index++;
                    }
                DragAndDrop.AcceptDrag(); GetWindow<SkillTreeGraphWindow>().Rebuild();
            });
        }
        private void DrawCoordinates()
        {
            if (Event.current.type != EventType.Repaint) return;
            float width = contentRect.width, height = contentRect.height;
            float zoom = contentViewContainer.resolvedStyle.scale.value.x * spacingScale;
            if (width <= 0 || height <= 0 || zoom <= 0) return;
            Vector2 origin = contentViewContainer.ChangeCoordinatesTo(this, Vector2.zero);
            // Keep the labels legible when zoomed out. Values remain in saved layout units.
            float step = 100;
            while (step * zoom < 70) step *= 2;
            float spacing = step * zoom;
            var previousColor = Handles.color;
            Handles.BeginGUI();
            for (float x = Mathf.Ceil(-origin.x / spacing) * spacing + origin.x; x < width; x += spacing)
            {
                Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, .10f) : new Color(0, 0, 0, .12f);
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, height));
                GUI.Label(new Rect(x + 3, 2, 85, 20), Mathf.Round((x - origin.x) / zoom).ToString("0"), EditorStyles.miniLabel);
            }
            for (float y = Mathf.Ceil(-origin.y / spacing) * spacing + origin.y; y < height; y += spacing)
            {
                Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, .10f) : new Color(0, 0, 0, .12f);
                Handles.DrawLine(new Vector3(0, y), new Vector3(width, y));
                GUI.Label(new Rect(3, y + 3, 85, 20), Mathf.Round((origin.y - y) / zoom).ToString("0"), EditorStyles.miniLabel);
            }
            Handles.color = new Color(.45f, .75f, 1, .65f);
            if (origin.x >= 0 && origin.x < width) Handles.DrawLine(new Vector3(origin.x, 0), new Vector3(origin.x, height));
            if (origin.y >= 0 && origin.y < height) Handles.DrawLine(new Vector3(0, origin.y), new Vector3(width, origin.y));
            Handles.EndGUI();
            Handles.color = previousColor;
            GUI.Label(new Rect(width - 130, 2, 128, 20), "X → / Y ↑   Grid: " + step.ToString("0"), EditorStyles.miniLabel);
        }
        public override List<Port> GetCompatiblePorts(Port start, NodeAdapter adapter)
            => ports.Where(port => port.direction != start.direction && port.node != start.node).ToList();
        private GraphViewChange Changed(GraphViewChange change)
        {
            if (change.movedElements != null)
                foreach (var element in change.movedElements)
                    if (element is Card card)
                    {
                        var rect = card.GetPosition();
                        card.SetPosition(rect);
                        SkillTreeGraphEditing.Move(data, card.data, new Vector2(rect.x, -rect.y) / spacingScale);
                    }
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
