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
    [SerializeField] private Vector2 positionScale = Vector2.one;
    [SerializeField] private float nodeWidth = 220f;
    [SerializeField] private float maximumZoom = 10f;
    [SerializeField] private bool showComposer;
    [SerializeField] private SkillNodeData centerNode;
    [SerializeField] private SkillTreeLayoutComposer.Settings composition = new SkillTreeLayoutComposer.Settings();
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
        bool gridMode = layout != null && layout.gridPlacement;
        if (layout != null)
        {
            var gridOptions = new Toolbar();
            var gridToggle = new Toggle("Grid Placement") { value = gridMode };
            gridToggle.RegisterValueChangedCallback(evt =>
            {
                SkillTreeGraphEditing.SetGridMode(layout, evt.newValue);
                Rebuild();
            });
            gridOptions.Add(gridToggle);
            if (gridMode)
            {
                AddGridSpacing("Cell W / H", layout.gridCellSize, true);
                AddGridSpacing("Same-cell Gap X / Y", layout.gridNodeGap, false);
            }
            rootVisualElement.Add(gridOptions);

            void AddGridSpacing(string label, Vector2 value, bool cellSize)
            {
                var spacing = new Vector2Field(label) { value = value };
                spacing.RegisterValueChangedCallback(evt =>
                {
                    var next = evt.newValue;
                    if (!float.IsFinite(next.x) || !float.IsFinite(next.y))
                    {
                        spacing.SetValueWithoutNotify(evt.previousValue);
                        return;
                    }
                    SkillTreeGraphEditing.SetGridSpacing(layout,
                        cellSize ? next : layout.gridCellSize, cellSize ? layout.gridNodeGap : next);
                    spacing.SetValueWithoutNotify(cellSize ? layout.gridCellSize : layout.gridNodeGap);
                    graph?.RefreshGrid();
                });
                gridOptions.Add(spacing);
            }
        }
        var viewOptions = new Toolbar();
        var displaySpacing = new FloatField("Spacing Multiplier") { value = spacingScale, isDelayed = true };
        displaySpacing.style.width = 280;
        displaySpacing.tooltip = "Visual spacing only; saved node coordinates remain unchanged.";
        displaySpacing.RegisterValueChangedCallback(evt => { if (float.IsFinite(evt.newValue)) spacingScale = Mathf.Clamp(evt.newValue, .1f, 20f); Rebuild(); });
        displaySpacing.SetEnabled(!gridMode);
        viewOptions.Add(displaySpacing);
        rootVisualElement.Add(viewOptions);
        viewOptions = new Toolbar();
        void AddAxisScale(string label, bool xAxis)
        {
            var field = new FloatField(label) { value = xAxis ? positionScale.x : positionScale.y, isDelayed = true };
            field.style.width = 210;
            field.labelElement.style.minWidth = 60;
            field.labelElement.style.width = 60;
            field.RegisterValueChangedCallback(evt =>
            {
                if (!float.IsFinite(evt.newValue) || evt.newValue <= 0)
                { field.SetValueWithoutNotify(xAxis ? positionScale.x : positionScale.y); return; }
                if (xAxis) positionScale.x = evt.newValue; else positionScale.y = evt.newValue;
            });
            viewOptions.Add(field);
        }
        AddAxisScale("Scale X", true);
        AddAxisScale("Scale Y", false);
        viewOptions.Add(new ToolbarButton(() =>
        {
            if (layout == null || layout.gridPlacement || !float.IsFinite(positionScale.x) || !float.IsFinite(positionScale.y) || positionScale.x <= 0 || positionScale.y <= 0) return;
            Undo.RecordObject(layout, "Scale skill node positions");
            foreach (var entry in layout.nodeLayout)
                if (entry != null) entry.position = Vector2.Scale(entry.position, positionScale);
            layout.revision++;
            EditorUtility.SetDirty(layout);
            Rebuild();
        }) { text = "Apply Position Scale" });
        var zoomLimit = new FloatField("Maximum Zoom") { value = maximumZoom, isDelayed = true };
        zoomLimit.style.width = 250;
        zoomLimit.RegisterValueChangedCallback(evt => { if (float.IsFinite(evt.newValue)) maximumZoom = Mathf.Clamp(evt.newValue, 2f, 30f); Rebuild(); });
        viewOptions.Add(zoomLimit);
        rootVisualElement.Add(viewOptions);
        var sizeOptions = new Toolbar();
        var widthField = new FloatField("Node Width") { value = nodeWidth, isDelayed = true };
        widthField.style.width = 250;
        widthField.RegisterValueChangedCallback(evt => { if (float.IsFinite(evt.newValue)) nodeWidth = Mathf.Clamp(evt.newValue, 180f, 600f); Rebuild(); });
        widthField.SetEnabled(!gridMode);
        sizeOptions.Add(widthField);
        rootVisualElement.Add(sizeOptions);
        if (layout != null) AddLayoutComposer(gridMode);
        graph = new Canvas(layout, gridMode ? 1f : spacingScale, maximumZoom, gridMode ? 220f : nodeWidth); graph.style.flexGrow = 1; rootVisualElement.Add(graph);
    }
    private void AddLayoutComposer(bool gridMode)
    {
        var foldout = new Foldout { text = "Compose Layout", value = showComposer };
        foldout.RegisterValueChangedCallback(evt => showComposer = evt.newValue);
        rootVisualElement.Add(foldout);
        if (gridMode)
        {
            foldout.Add(new HelpBox("Switch off Grid Placement to compose free-form positions.", HelpBoxMessageType.Info));
            return;
        }

        SkillTreeLayoutComposer composer;
        try { composer = new SkillTreeLayoutComposer(layout, centerNode); }
        catch (System.InvalidOperationException error)
        {
            foldout.Add(new HelpBox(error.Message, HelpBoxMessageType.Error));
            return;
        }
        if (composer.Root == null) return;

        var panel = new ScrollView { style = { maxHeight = 335 } };
        foldout.Add(panel);
        var center = new ObjectField("Center Node") { objectType = typeof(SkillNodeData), value = composer.Root };
        center.tooltip = "Select a node with no prerequisites inside this graph.";
        center.RegisterValueChangedCallback(evt =>
        {
            centerNode = evt.newValue as SkillNodeData;
            Rebuild();
        });
        panel.Add(center);
        AddMixFields(panel, composition.mix);
        var seed = new IntegerField("Seed") { value = composition.seed };
        seed.RegisterValueChangedCallback(evt => composition.seed = evt.newValue);
        panel.Add(seed);
        AddNumber(panel, "Jitter", composition.jitter, 0, 2, value => composition.jitter = value);
        AddNumber(panel, "Spacing", composition.spacing, 40, 5000, value => composition.spacing = value);
        AddNumber(panel, "Clearance", composition.clearance, 0, 1000, value => composition.clearance = value);
        AddNumber(panel, "Rotation", composition.rotation, -360, 360, value => composition.rotation = value);
        AddNumber(panel, "Sweep", composition.sweep, 10, 360, value => composition.sweep = value);
        AddNumber(panel, "Branch Spread", composition.spread, 0, 10, value => composition.spread = value);
        AddNumber(panel, "Twist (Radians)", composition.twist, -3.14f, 3.14f, value => composition.twist = value);
        if (composer.Heads.Count > 0)
        {
            var branches = new Foldout { text = "Branch Overrides", value = false };
            panel.Add(branches);
            foreach (var head in composer.Heads)
            {
                var item = new Foldout { text = head.name, value = false };
                branches.Add(item);
                var branchSettings = composition.branches.FirstOrDefault(value => value.head == head);
                var enabled = new Toggle("Use Custom Mix") { value = branchSettings != null };
                item.Add(enabled);
                var controls = new VisualElement();
                item.Add(controls);
                void RefreshBranch()
                {
                    controls.Clear();
                    if (branchSettings == null) return;
                    AddMixFields(controls, branchSettings.mix);
                    AddNumber(controls, "Angle Offset", branchSettings.angleOffset, -360, 360,
                        value => branchSettings.angleOffset = value);
                    AddNumber(controls, "Spacing Multiplier", branchSettings.spacingMultiplier, .1f, 5,
                        value => branchSettings.spacingMultiplier = value);
                }
                enabled.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        branchSettings = new SkillTreeLayoutComposer.Branch { head = head };
                        branchSettings.mix.radialConstellationFan = composition.mix.radialConstellationFan;
                        branchSettings.mix.clusterSpiralDiamond = composition.mix.clusterSpiralDiamond;
                        composition.branches.Add(branchSettings);
                    }
                    else
                    {
                        composition.branches.Remove(branchSettings);
                        branchSettings = null;
                    }
                    RefreshBranch();
                });
                RefreshBranch();
            }
        }

        var actions = new Toolbar();
        foldout.Add(actions);
        actions.Add(new ToolbarButton(() => seed.value = UnityEngine.Random.Range(0, int.MaxValue)) { text = "Randomize Seed" });
        actions.Add(new ToolbarButton(() =>
        {
            try
            {
                var current = new SkillTreeLayoutComposer(layout, centerNode);
                var positions = current.Calculate(composition, new Vector2(nodeWidth, 220f) / spacingScale);
                SkillTreeGraphEditing.ApplyComposedLayout(layout, positions);
                Rebuild();
                graph?.FrameAll();
            }
            catch (System.InvalidOperationException error)
            {
                EditorUtility.DisplayDialog("Compose Layout", error.Message, "OK");
            }
        }) { text = "Apply Layout" });
    }

    private static void AddMixFields(VisualElement parent, SkillTreeLayoutComposer.Mix mix)
    {
        AddWeights("Radial / Constellation / Fan", mix.radialConstellationFan, value => mix.radialConstellationFan = value);
        AddWeights("Cluster / Spiral / Diamond", mix.clusterSpiralDiamond, value => mix.clusterSpiralDiamond = value);

        void AddWeights(string label, Vector3 value, System.Action<Vector3> changed)
        {
            var field = new Vector3Field(label) { value = value };
            field.labelElement.style.minWidth = 185;
            field.tooltip = "X, Y and Z are the weights for the three listed shapes.";
            field.RegisterValueChangedCallback(evt =>
            {
                var next = evt.newValue;
                if (!float.IsFinite(next.x) || !float.IsFinite(next.y) || !float.IsFinite(next.z))
                    next = evt.previousValue;
                next = Vector3.Min(Vector3.one * 100, Vector3.Max(Vector3.zero, next));
                field.SetValueWithoutNotify(next);
                changed(next);
            });
            parent.Add(field);
        }
    }

    private static void AddNumber(VisualElement parent, string label, float value, float minimum, float maximum,
        System.Action<float> changed)
    {
        var field = new FloatField(label) { value = value, isDelayed = true };
        field.RegisterValueChangedCallback(evt =>
        {
            float next = float.IsFinite(evt.newValue) ? Mathf.Clamp(evt.newValue, minimum, maximum) : evt.previousValue;
            field.SetValueWithoutNotify(next);
            changed(next);
        });
        parent.Add(field);
    }

    private sealed class Card : Node
    {
        public SkillNodeData data;
        public Port input, output;
        private readonly Vector2Field positionField;
        private readonly Vector2IntField gridField;
        private readonly SkillTreeLayoutData layoutData;
        private readonly float spacingScale;
        public Card(SkillNodeData data, SkillTreeLayoutData layout, float spacingScale, float nodeWidth, System.Action<SkillNodeData, Vector2Int> moveInGrid)
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
            positionField.style.width = nodeWidth - 24;
            positionField.style.flexDirection = FlexDirection.Column;
            positionField.labelElement.style.minWidth = 60;
            positionField.labelElement.style.width = 60;
            style.minWidth = nodeWidth;
            style.width = nodeWidth;
            positionField.RegisterValueChangedCallback(evt =>
            {
                if (!float.IsFinite(evt.newValue.x) || !float.IsFinite(evt.newValue.y))
                {
                    positionField.SetValueWithoutNotify(evt.previousValue);
                    return;
                }
                SetLayoutPosition(evt.newValue);
            });
            var details = new Foldout { text = "Position / Rewards", value = false };
            if (layout.gridPlacement)
            {
                gridField = new Vector2IntField("Grid X / Y");
                gridField.RegisterValueChangedCallback(evt => moveInGrid(data, evt.newValue));
                details.Add(gridField);
            }
            else details.Add(positionField);
            details.Add(button);
            var level = new IntegerField("Level") { value = data.level, isDelayed = true };
            level.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(data, "Set skill node level"); data.level = Mathf.Max(1, evt.newValue);
                level.SetValueWithoutNotify(data.level); EditorUtility.SetDirty(data);
            });
            details.Add(level);
            extensionContainer.Add(details); RefreshExpandedState(); RefreshPorts();
        }
        public void SetLayoutPosition(Vector2 position)
        {
            SkillTreeGraphEditing.Move(layoutData, data, position);
            var rect = GetPosition();
            rect.position = new Vector2(position.x, -position.y) * spacingScale;
            SetPosition(rect);
        }
        public void ApplyPlacement(SkillNodePlacement entry)
        {
            var rect = GetPosition();
            rect.position = new Vector2(entry.position.x, -entry.position.y) * spacingScale;
            SetPosition(rect);
            gridField?.SetValueWithoutNotify(entry.gridPosition);
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
        private readonly Dictionary<SkillNodeData, Card> cards = new Dictionary<SkillNodeData, Card>();
        private readonly IMGUIContainer coordinates;
        public Canvas(SkillTreeLayoutData data) : this(data, 1f, 10f) { }
        public Canvas(SkillTreeLayoutData data, float spacingScale, float maximumZoom) : this(data, spacingScale, maximumZoom, 220f) { }
        public Canvas(SkillTreeLayoutData data, float spacingScale, float maximumZoom, float nodeWidth)
        {
            this.data = data; this.spacingScale = spacingScale; SetupZoom(.05f, maximumZoom);
            this.AddManipulator(new ContentDragger()); this.AddManipulator(new SelectionDragger()); this.AddManipulator(new RectangleSelector());
            coordinates = new IMGUIContainer(DrawCoordinates) { pickingMode = PickingMode.Ignore };
            Insert(0, coordinates);
            coordinates.StretchToParentSize();
            viewTransformChanged += _ => coordinates.MarkDirtyRepaint();
            RegisterCallback<GeometryChangedEvent>(_ => coordinates.MarkDirtyRepaint());
            if (data == null) return;
            foreach (var node in data.nodeDatas)
            {
                if (node == null || cards.ContainsKey(node)) continue;
                var card = new Card(node, data, spacingScale, nodeWidth, MoveCardToGrid); var saved = data.nodeLayout.Find(value => value != null && value.node == node);
                var position = saved != null ? saved.position : Vector2.zero;
                card.SetPosition(new Rect(position.x * spacingScale, -position.y * spacingScale, nodeWidth, 100)); AddElement(card); cards.Add(node, card);
                if (saved != null) card.ApplyPlacement(saved);
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
        private void MoveCardToGrid(SkillNodeData node, Vector2Int position)
        {
            SkillTreeGraphEditing.MoveToGrid(data, new[] { new KeyValuePair<SkillNodeData, Vector2Int>(node, position) });
            RefreshGrid();
        }

        public void RefreshGrid()
        {
            if (data == null || !data.gridPlacement) return;
            foreach (var entry in data.nodeLayout)
                if (entry != null && entry.node != null && cards.TryGetValue(entry.node, out var card) && card.parent != null)
                    card.ApplyPlacement(entry);
            coordinates.MarkDirtyRepaint();
        }

        private void DrawCoordinates()
        {
            if (Event.current.type != EventType.Repaint) return;
            float width = contentRect.width, height = contentRect.height;
            float zoom = contentViewContainer.resolvedStyle.scale.value.x * spacingScale;
            if (width <= 0 || height <= 0 || zoom <= 0) return;
            Vector2 origin = contentViewContainer.ChangeCoordinatesTo(this, Vector2.zero);
            // Keep the labels legible when zoomed out. Values remain in saved layout units.
            bool gridMode = data != null && data.gridPlacement;
            Vector2 pitch = gridMode ? SkillTreeGraphEditing.GridPitch(data) : new Vector2(100, 100);
            Vector2 steps = pitch * zoom;
            while (steps.x < 70) steps.x *= 2;
            while (steps.y < 70) steps.y *= 2;
            Vector2 units = gridMode ? pitch : Vector2.one;
            var previousColor = Handles.color;
            Handles.BeginGUI();
            for (float x = Mathf.Ceil(-origin.x / steps.x) * steps.x + origin.x; x < width; x += steps.x)
            {
                Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, .10f) : new Color(0, 0, 0, .12f);
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, height));
                GUI.Label(new Rect(x + 3, 2, 85, 20), Mathf.Round((x - origin.x) / (zoom * units.x)).ToString("0"), EditorStyles.miniLabel);
            }
            for (float y = Mathf.Ceil(-origin.y / steps.y) * steps.y + origin.y; y < height; y += steps.y)
            {
                Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, .10f) : new Color(0, 0, 0, .12f);
                Handles.DrawLine(new Vector3(0, y), new Vector3(width, y));
                GUI.Label(new Rect(3, y + 3, 85, 20), Mathf.Round((origin.y - y) / (zoom * units.y)).ToString("0"), EditorStyles.miniLabel);
            }
            Handles.color = new Color(.45f, .75f, 1, .65f);
            if (origin.x >= 0 && origin.x < width) Handles.DrawLine(new Vector3(origin.x, 0), new Vector3(origin.x, height));
            if (origin.y >= 0 && origin.y < height) Handles.DrawLine(new Vector3(0, origin.y), new Vector3(width, origin.y));
            Handles.EndGUI();
            Handles.color = previousColor;
            GUI.Label(new Rect(width - 130, 2, 128, 20), gridMode ? "Grid X → / Y ↑" : "X → / Y ↑", EditorStyles.miniLabel);
        }
        public override List<Port> GetCompatiblePorts(Port start, NodeAdapter adapter)
            => ports.Where(port => port.direction != start.direction && port.node != start.node).ToList();
        private GraphViewChange Changed(GraphViewChange change)
        {
            if (change.movedElements != null)
            {
                var gridChanges = new List<KeyValuePair<SkillNodeData, Vector2Int>>();
                Vector2 pitch = data.gridPlacement ? SkillTreeGraphEditing.GridPitch(data) : Vector2.one;
                foreach (var element in change.movedElements)
                {
                    if (!(element is Card card)) continue;
                    var rect = card.GetPosition();
                    Vector2 position = new Vector2(rect.x, -rect.y) / spacingScale;
                    if (data.gridPlacement)
                    {
                        var saved = data.nodeLayout.Find(entry => entry != null && entry.node == card.data);
                        var cell = saved == null ? SkillTreeGraphEditing.GridCoordinate(position, pitch)
                            : saved.gridPosition + SkillTreeGraphEditing.GridCoordinate(position - saved.position, pitch);
                        gridChanges.Add(new KeyValuePair<SkillNodeData, Vector2Int>(card.data, cell));
                    }
                    else SkillTreeGraphEditing.Move(data, card.data, position);
                }
                if (gridChanges.Count > 0)
                {
                    SkillTreeGraphEditing.MoveToGrid(data, gridChanges);
                    schedule.Execute(RefreshGrid);
                }
            }
            if (change.elementsToRemove != null)
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge) SkillTreeGraphEditing.Disconnect(((Card)edge.output.node).data, ((Card)edge.input.node).data);
                    if (element is Card card) SkillTreeGraphEditing.Remove(data, card.data);
                }
            if (change.elementsToRemove != null && data.gridPlacement) schedule.Execute(RefreshGrid);
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
