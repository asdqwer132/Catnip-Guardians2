using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// Same GraphView interaction as the skill tree: nodes, ports, edges, box selection and zoom.
internal sealed class MergeRecipeGraphCanvas : GraphView
{
    private sealed class Card : Node
    {
        public readonly MergeGraphItemNode data;
        public readonly Port output;
        public readonly List<Port> inputs = new List<Port>();
        private readonly Action<MergeGraphItemNode> onSelected;
        private readonly Vector2Field position;
        private readonly Image icon;
        public Card(MergeGraphItemNode data, MergeRecipeGraphAsset graph, Action<MergeGraphItemNode> selected, Action rebuild)
        {
            this.data = data; onSelected = selected;
            style.width = 240;
            output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(MergeItem));
            output.portName = "Result"; outputContainer.Add(output);
            for (int i = 0; i < data.ingredients.Count; i++)
            {
                int slot = i;
                var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(MergeItem));
                port.portName = "Ingredient " + (i + 1); port.userData = slot;
                inputs.Add(port); inputContainer.Add(port);
                var remove = new Button(() => { MergeRecipeGraphEditing.RemoveInput(graph, data, slot); rebuild(); }) { text = "−" };
                port.Add(remove);
            }
            inputContainer.Add(new Button(() => { MergeRecipeGraphEditing.AddInput(graph, data); rebuild(); }) { text = "+ Ingredient" });
            icon = new Image { scaleMode = ScaleMode.ScaleToFit }; icon.style.height = 32; icon.style.width = 32;
            titleContainer.Insert(0, icon);
            var details = new Foldout { text = "Position / Item", value = false };
            if (graph.placementMode == MergeGraphPlacementMode.Grid)
            {
                var coordinate = new Vector2IntField("Grid X / Y") { value = data.gridPosition };
                coordinate.style.flexDirection = FlexDirection.Column;
                coordinate.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(graph, "Move merge node to cell"); data.gridPosition = evt.newValue;
                    MergeRecipeGraphLayout.Arrange(graph); rebuild();
                }); details.Add(coordinate);
            }
            else
            {
                position = new Vector2Field("Position"); position.style.flexDirection = FlexDirection.Column;
                position.RegisterValueChangedCallback(evt =>
                {
                    if (!float.IsFinite(evt.newValue.x) || !float.IsFinite(evt.newValue.y)) return;
                    Undo.RecordObject(graph, "Position merge node"); data.position = evt.newValue;
                    EditorUtility.SetDirty(graph); var rect = GetPosition(); rect.position = evt.newValue; SetPosition(rect);
                }); details.Add(position);
            }
            details.Add(new Button(() => { Selection.activeObject = data.item; EditorGUIUtility.PingObject(data.item); }) { text = "Inspect Merge Item" });
            extensionContainer.Add(details);
            Refresh(); RefreshExpandedState(); RefreshPorts();
        }
        public void Refresh()
        {
            title = data.item != null ? data.item.name : "Merge Item";
            icon.sprite = data.item != null && data.item.item != null ? data.item.item.icon : null;
        }
        public override void SetPosition(Rect rect) { base.SetPosition(rect); position?.SetValueWithoutNotify(rect.position); }
        public override void OnSelected() { base.OnSelected(); onSelected(data); schedule.Execute(() => onSelected(data)); }
    }
    private readonly Action<MergeGraphItemNode> select;
    private readonly Action<Object[]> import;
    private MergeRecipeGraphAsset graph;
    private bool pendingGridSnap;
    public Vector2 Center => contentViewContainer.WorldToLocal(this.LocalToWorld(contentRect.center));
    internal List<MergeGraphItemNode> SelectedNodes => selection.OfType<Card>().Select(card => card.data).ToList();
    internal int SelectionCount => selection.OfType<Card>().Count();
    internal MergeGraphItemNode PrimarySelection => selection.OfType<Card>().LastOrDefault()?.data;

    public MergeRecipeGraphCanvas(Action<MergeGraphItemNode> select, Action<Vector2> createItem, Action<Object[]> import, Action<string> status)
    {
        this.select = select; this.import = import;
        style.flexGrow = 1; style.minWidth = 300;
        SetupZoom(.05f, 10f);
        RegisterCallback<PointerUpEvent>(_ => schedule.Execute(() =>
        {
            if (pendingGridSnap && graph != null)
            { pendingGridSnap = false; MergeRecipeGraphLayout.Arrange(graph); SetGraph(graph, PrimarySelection); }
            select(PrimarySelection);
        }), TrickleDown.TrickleDown);
        this.AddManipulator(new ContentDragger()); this.AddManipulator(new SelectionDragger()); this.AddManipulator(new RectangleSelector());
        var grid = new IMGUIContainer(DrawGrid) { pickingMode = PickingMode.Ignore };
        Insert(0, grid); grid.StretchToParentSize();
        viewTransformChanged += _ => grid.MarkDirtyRepaint();
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            if (evt.target is Card || (evt.target as VisualElement)?.GetFirstAncestorOfType<Card>() != null) return;
            var point = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction("+ Merge Item", _ => createItem(point));
        }));
        serializeGraphElements = elements => MergeRecipeGraphClipboard.Copy(graph, elements.OfType<Card>().Select(card => card.data));
        canPasteSerializedData = MergeRecipeGraphClipboard.CanPaste;
        unserializeAndPaste = (_, text) =>
        {
            if (graph == null) return;
            var added = MergeRecipeGraphClipboard.Paste(graph, text);
            SetGraph(graph, added.LastOrDefault()); select(PrimarySelection);
        };
        RegisterCallback<DragUpdatedEvent>(evt =>
        {
            if (DragAndDrop.objectReferences.Any(asset => asset is ItemData || asset is MergeItem || asset is ItemRecipeData || asset is MergeRecipeBook))
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        });
        RegisterCallback<DragPerformEvent>(evt => { DragAndDrop.AcceptDrag(); this.import(DragAndDrop.objectReferences); evt.StopPropagation(); });
    }
    public void SetGraph(MergeRecipeGraphAsset value, MergeGraphItemNode selected)
    {
        var selectedIds = selection.OfType<Card>().Select(card => card.data.id).ToHashSet();
        if (graph != value) selectedIds.Clear();
        graphViewChanged = null;
        ClearSelection(); foreach (var element in graphElements.ToList()) RemoveElement(element);
        graph = value;
        if (graph == null) return;
        MergeRecipeGraphEditing.Upgrade(graph);
        if (graph.placementMode == MergeGraphPlacementMode.Grid) MergeRecipeGraphLayout.Arrange(graph);
        var cards = new Dictionary<MergeGraphItemNode, Card>();
        foreach (var node in graph.items)
        {
            var card = new Card(node, graph, select, () => SetGraph(graph, node));
            card.SetPosition(new Rect(node.position, new Vector2(240, 100))); AddElement(card); cards.Add(node, card);
        }
        foreach (var pair in cards)
            for (int i = 0; i < pair.Key.ingredients.Count; i++)
            {
                var source = MergeRecipeGraphEditing.InputNode(graph, pair.Key, i);
                if (source != null && cards.TryGetValue(source, out var start)) AddElement(start.output.ConnectTo(pair.Value.inputs[i]));
            }
        foreach (var pair in cards)
            if (selectedIds.Contains(pair.Key.id) || pair.Key == selected) AddToSelection(pair.Value);
        graphViewChanged = Changed;
    }
    private void DrawGrid()
    {
        if (Event.current.type != EventType.Repaint) return;
        float zoom = contentViewContainer.resolvedStyle.scale.value.x;
        if (zoom <= 0) return;
        Vector2 origin = contentViewContainer.ChangeCoordinatesTo(this, Vector2.zero);
        Vector2 pitch = graph != null && graph.placementMode == MergeGraphPlacementMode.Grid ? MergeRecipeGraphLayout.Pitch(graph) : new Vector2(100, 100);
        float labelUnit = graph != null && graph.placementMode == MergeGraphPlacementMode.Grid ? 1 : 100;
        Vector2 screen = pitch * zoom;
        if (screen.x < 10 || screen.y < 10) return;
        var old = Handles.color; Handles.BeginGUI(); Handles.color = new Color(.5f, .5f, .5f, .25f);
        for (float x = Mathf.Ceil(-origin.x / screen.x) * screen.x + origin.x; x < contentRect.width; x += screen.x)
        { Handles.DrawLine(new Vector3(x, 0), new Vector3(x, contentRect.height)); GUI.Label(new Rect(x + 3, 0, 60, 20), Mathf.Round((x - origin.x) / screen.x * labelUnit).ToString("0"), EditorStyles.miniLabel); }
        for (float y = Mathf.Ceil(-origin.y / screen.y) * screen.y + origin.y; y < contentRect.height; y += screen.y)
        { Handles.DrawLine(new Vector3(0, y), new Vector3(contentRect.width, y)); GUI.Label(new Rect(0, y + 3, 60, 20), Mathf.Round((y - origin.y) / screen.y * labelUnit).ToString("0"), EditorStyles.miniLabel); }
        Handles.EndGUI(); Handles.color = old;
    }
    public void RefreshPreviews() { foreach (var card in nodes.OfType<Card>()) card.Refresh(); }
    public override List<Port> GetCompatiblePorts(Port start, NodeAdapter adapter)
        => ports.Where(port => port.direction != start.direction && port.node != start.node).ToList();
    private GraphViewChange Changed(GraphViewChange change)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return new GraphViewChange();
        if (change.movedElements != null)
        {
            Undo.RecordObject(graph, "Move merge nodes");
            Vector2 pitch = MergeRecipeGraphLayout.Pitch(graph);
            foreach (var card in change.movedElements.OfType<Card>())
            {
                card.data.position = card.GetPosition().position;
                if (graph.placementMode == MergeGraphPlacementMode.Grid)
                { card.data.gridPosition = MergeRecipeGraphLayout.Nearest(card.data.position, pitch); pendingGridSnap = true; }
            }
            EditorUtility.SetDirty(graph);
        }
        if (change.elementsToRemove != null)
        {
            foreach (var edge in change.elementsToRemove.OfType<Edge>())
            {
                var target = (Card)edge.input.node;
                MergeRecipeGraphEditing.SetInput(graph, target.data, (int)edge.input.userData, null);
            }
            foreach (var card in change.elementsToRemove.OfType<Card>()) MergeRecipeGraphEditing.RemoveItem(graph, card.data);
        }
        if (change.edgesToCreate != null)
            foreach (var edge in change.edgesToCreate)
            {
                var source = (Card)edge.output.node; var target = (Card)edge.input.node;
                MergeRecipeGraphEditing.SetInput(graph, target.data, (int)edge.input.userData, source.data.item, source.data.id);
            }
        if (change.elementsToRemove != null) schedule.Execute(() => select(PrimarySelection));
        return change;
    }
}
