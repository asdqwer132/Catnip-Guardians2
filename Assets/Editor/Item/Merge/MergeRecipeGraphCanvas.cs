using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

internal sealed class MergeRecipeGraphCanvas : VisualElement
{
    private sealed class Card : VisualElement
    {
        public MergeGraphItemNode node;
        public Label title, detail;
        public Image icon;
        public IntegerField tier;
        public readonly List<Label> ingredients = new List<Label>();
        public float Height => 140 + node.ingredients.Count * 30;
    }
    private sealed class Port : VisualElement
    {
        public Card card;
        public int slot; // -1은 출력, 0 이상은 N개 재료 입력의 인덱스입니다.
    }
    private readonly VisualElement content = new VisualElement();
    private readonly List<Card> cards = new List<Card>();
    private readonly List<Port> ports = new List<Port>();
    private readonly Action<MergeGraphItemNode> select;
    private readonly Action<Vector2> createItem;
    private readonly Action<Object[]> import;
    private readonly Action<string> status;
    private MergeRecipeGraphAsset graph;
    private Card selected;
    private readonly HashSet<string> selectedIds = new HashSet<string>();
    private readonly HashSet<string> rectangleBase = new HashSet<string>();
    private readonly HashSet<string> rectangleSaved = new HashSet<string>();
    private string rectanglePrimary;
    private Port linking;
    private Vector2 pan = new Vector2(40, 40), pointer, rectangleStart;
    private float zoom = 1;
    private bool panning, dragging, selectingRectangle;
    private int capturedPointer, dragUndoGroup = -1;
    public Vector2 Center => (contentRect.size * .5f - pan) / zoom;
    internal int SelectionCount => selectedIds.Count;
    internal MergeGraphItemNode PrimarySelection => selected?.node;
    internal bool IsSelected(MergeGraphItemNode node) => node != null && selectedIds.Contains(node.id);

    public MergeRecipeGraphCanvas(Action<MergeGraphItemNode> select, Action<Vector2> createItem, Action<Object[]> import, Action<string> status)
    {
        this.select = select; this.createItem = createItem; this.import = import; this.status = status;
        style.flexGrow = 1; style.minWidth = 300; style.overflow = Overflow.Hidden;
        style.backgroundColor = new Color(.105f, .115f, .13f); focusable = true;
        content.style.position = Position.Absolute; content.style.width = content.style.height = 100000;
        content.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0), 0);
        Add(content);
        generateVisualContent += Paint;
        RegisterCallback<PointerDownEvent>(Down);
        RegisterCallback<PointerMoveEvent>(Move);
        RegisterCallback<PointerUpEvent>(Up);
        RegisterCallback<PointerCaptureOutEvent>(evt => { if (evt.target == this) FinishGesture(); });
        RegisterCallback<DetachFromPanelEvent>(_ => Release());
        RegisterCallback<WheelEvent>(Wheel);
        RegisterCallback<KeyDownEvent>(Key);
        RegisterCallback<ValidateCommandEvent>(evt =>
        {
            if (CanHandleCommand(evt.commandName, evt.target as VisualElement)) evt.StopPropagation();
        });
        RegisterCallback<ExecuteCommandEvent>(evt =>
        {
            if (!CanHandleCommand(evt.commandName, evt.target as VisualElement)) return;
            switch (evt.commandName)
            {
                case "Copy": CopySelectedNodes(); break;
                case "Paste": PasteNodes(EditorGUIUtility.systemCopyBuffer); break;
                case "Duplicate": DuplicateSelectedNodes(); break;
                case "SelectAll": SelectAllNodes(); break;
                case "Delete": case "SoftDelete": RemoveSelectedNodes(); break;
            }
            evt.StopPropagation();
        });
        RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var element = evt.target as VisualElement;
            if (element is Card || element?.GetFirstAncestorOfType<Card>() != null) return;
            Vector2 at = (this.WorldToLocal(evt.mousePosition) - pan) / zoom;
            evt.menu.AppendAction("+ 머지 아이템 생성", _ => createItem(at));
            evt.menu.AppendAction("노드 붙여넣기", _ => PasteNodes(EditorGUIUtility.systemCopyBuffer));
        }));
        RegisterCallback<DragUpdatedEvent>(evt =>
        {
            foreach (var asset in DragAndDrop.objectReferences)
                if (asset is ItemData || asset is MergeItem || asset is ItemRecipeData || asset is MergeRecipeBook)
                { DragAndDrop.visualMode = DragAndDropVisualMode.Link; evt.StopPropagation(); break; }
        });
        RegisterCallback<DragPerformEvent>(evt => { DragAndDrop.AcceptDrag(); import(DragAndDrop.objectReferences); evt.StopPropagation(); });
        TransformContent();
    }
    public void SetGraph(MergeRecipeGraphAsset value, MergeGraphItemNode selection)
    {
        Release();
        if (graph != value) selectedIds.Clear();
        graph = value;
        if (graph != null) MergeRecipeGraphEditing.Upgrade(graph);
        // Undo와 입력 개수 변경으로 노드 객체가 다시 만들어져도 ID로 선택을 유지합니다.
        selectedIds.RemoveWhere(id => graph == null || !graph.items.Exists(node => node.id == id));
        if (selection != null && !selectedIds.Contains(selection.id))
        { selectedIds.Clear(); selectedIds.Add(selection.id); }
        content.Clear(); cards.Clear(); ports.Clear(); selected = null;
        if (graph != null) foreach (var node in graph.items)
        {
            var card = AddCard(node);
            if (node.id == selection?.id) selected = card;
        }
        if (selected == null) selected = cards.Find(card => selectedIds.Contains(card.node.id));
        UpdateSelection(); RefreshPreviews(); MarkDirtyRepaint();
    }
    private static void Place(VisualElement element, float x, float y, float width, float height)
    {
        element.style.position = Position.Absolute; element.style.left = x; element.style.top = y;
        element.style.width = width; element.style.height = height;
    }
    private Card AddCard(MergeGraphItemNode node)
    {
        var card = new Card { name = "merge-item-node", node = node };
        Place(card, node.position.x, node.position.y, 240, card.Height);
        card.style.backgroundColor = new Color(.19f, .24f, .29f);
        card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
        card.style.borderLeftWidth = card.style.borderRightWidth = card.style.borderTopWidth = card.style.borderBottomWidth = 2;
        card.title = new Label { pickingMode = PickingMode.Ignore };
        card.title.style.unityFontStyleAndWeight = FontStyle.Bold;
        Place(card.title, 12, 8, 215, 24); card.Add(card.title);
        card.icon = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
        Place(card.icon, 14, 37, 42, 42); card.Add(card.icon);
        card.detail = new Label { pickingMode = PickingMode.Ignore };
        card.detail.style.fontSize = 11; card.detail.style.whiteSpace = WhiteSpace.Normal;
        Place(card.detail, 66, 35, 160, 47); card.Add(card.detail);
        AddPort(card, -1, 233, 52, "ItemOutput");
        card.tier = new IntegerField("티어") { value = node.tier, name = "NodeTier" };
        Place(card.tier, 12, 85, 215, 22); card.Add(card.tier);
        card.tier.RegisterValueChangedCallback(evt =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { card.tier.SetValueWithoutNotify(node.tier); return; }
            MergeRecipeGraphEditing.SetTier(graph, node, evt.newValue);
            card.tier.SetValueWithoutNotify(node.tier); SelectNode(node, keepSelectionIfSelected: true); RefreshPreviews();
        });
        for (int i = 0; i < node.ingredients.Count; i++)
        {
            int index = i; float y = 115 + i * 30;
            AddPort(card, i, -7, y, "Ingredient_" + i);
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.fontSize = 11;
            Place(label, 15, y - 2, 180, 22); card.Add(label); card.ingredients.Add(label);
            var remove = new Button(() =>
            {
                if (EditorApplication.isPlaying) return;
                MergeRecipeGraphEditing.RemoveInput(graph, node, index); Rebuild(node);
            }) { text = "×", tooltip = "이 재료 입력 제거" };
            Place(remove, 205, y - 3, 22, 22); card.Add(remove);
        }
        var add = new Button(() =>
        {
            if (EditorApplication.isPlaying) return;
            MergeRecipeGraphEditing.AddInput(graph, node); Rebuild(node);
        }) { text = "+ 재료 입력" };
        Place(add, 12, card.Height - 25, 215, 22); card.Add(add);
        card.RegisterCallback<PointerDownEvent>(evt =>
        {
            var element = evt.target as VisualElement;
            if (evt.button != 0 || evt.altKey || graph == null || EditorApplication.isPlaying || element is BaseField<int> || element?.GetFirstAncestorOfType<BaseField<int>>() != null || element is Button || element?.GetFirstAncestorOfType<Button>() != null) return;
            Focus();
            SelectNode(node, evt.shiftKey || evt.actionKey, keepSelectionIfSelected: true);
            if (!IsSelected(node)) { evt.StopPropagation(); return; }
            Undo.IncrementCurrentGroup(); dragUndoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Move selected merge nodes");
            dragging = true; pointer = this.WorldToLocal(evt.position); Capture(evt.pointerId); evt.StopPropagation();
        });
        card.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Project에서 보기", _ => { if (node.item != null) EditorGUIUtility.PingObject(node.item); });
            evt.menu.AppendAction("선택 노드 복사", _ => { SelectNode(node, keepSelectionIfSelected: true); CopySelectedNodes(); });
            evt.menu.AppendAction("선택 노드 복제", _ => { SelectNode(node, keepSelectionIfSelected: true); DuplicateSelectedNodes(); });
            evt.menu.AppendAction("선택 노드 제거 (에셋 유지)", _ =>
            {
                SelectNode(node, keepSelectionIfSelected: true); RemoveSelectedNodes();
            });
            evt.StopPropagation();
        }));
        card.RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        cards.Add(card); content.Add(card); return card;
    }
    private void AddPort(Card card, int slot, float x, float y, string name)
    {
        var port = new Port { name = name, card = card, slot = slot,
            tooltip = slot < 0 ? "재료 출력 · 원하는 입력으로 드래그" : "재료 " + (slot + 1) + " · 우클릭으로 해제" };
        Place(port, x, y, 14, 14);
        port.style.backgroundColor = slot < 0 ? new Color(.35f, .75f, 1) : new Color(.5f, .85f, .45f);
        port.style.borderTopLeftRadius = port.style.borderTopRightRadius = port.style.borderBottomLeftRadius = port.style.borderBottomRightRadius = 7;
        port.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0 || evt.altKey || graph == null || EditorApplication.isPlaying) return;
            Focus(); linking = port; pointer = this.WorldToLocal(evt.position); Capture(evt.pointerId);
            evt.StopPropagation(); MarkDirtyRepaint();
        });
        port.AddManipulator(new ContextualMenuManipulator(evt => { evt.menu.AppendAction("연결 해제", _ => Disconnect(port)); evt.StopPropagation(); }));
        ports.Add(port); card.Add(port);
    }
    private static string Name(MergeItem item) => item != null ? item.name : "미연결";
    public void RefreshPreviews()
    {
        foreach (var card in cards)
        {
            var item = card.node.item; var data = item != null ? item.item : null;
            card.title.text = Name(item); card.icon.sprite = data != null ? data.icon : null;
            card.detail.text = data != null ? $"{data.name}\n{data.series} · R {item.Radius:0.##}" : "ItemData를 연결하세요";
            card.tier.SetValueWithoutNotify(card.node.tier);
            card.style.backgroundColor = graph.GetTierColor(card.node.tier);
            for (int i = 0; i < card.ingredients.Count; i++)
                card.ingredients[i].text = (i + 1) + ": " + Name(card.node.ingredients[i]);
        }
        MarkDirtyRepaint();
    }
    private void Rebuild(MergeGraphItemNode asset) { SetGraph(graph, asset); select(PrimarySelection); }
    private Vector2 Point(Port port) => this.WorldToLocal(port.worldBound.center);
    private void TransformContent()
    {
        content.style.translate = new Translate(pan.x, pan.y); content.style.scale = new Scale(new Vector3(zoom, zoom, 1)); MarkDirtyRepaint();
    }
    private void Capture(int id) { capturedPointer = id; this.CapturePointer(id); }
    private void FinishGesture()
    {
        if (dragUndoGroup >= 0) Undo.CollapseUndoOperations(dragUndoGroup);
        dragUndoGroup = -1; linking = null; dragging = panning = selectingRectangle = false; MarkDirtyRepaint();
    }
    private void Release() { FinishGesture(); if (this.HasPointerCapture(capturedPointer)) this.ReleasePointer(capturedPointer); }
    private void Down(PointerDownEvent evt)
    {
        var element = evt.target as VisualElement;
        if (!evt.altKey && (element is BaseField<int> || element?.GetFirstAncestorOfType<BaseField<int>>() != null)) return;
        Focus();
        pointer = this.WorldToLocal(evt.position);
        if (evt.button == 2 || (evt.button == 0 && evt.altKey)) { panning = true; Capture(evt.pointerId); evt.StopPropagation(); }
        else if (evt.button == 0 && !(element is Card) && element?.GetFirstAncestorOfType<Card>() == null)
        {
            rectangleSaved.Clear(); rectangleSaved.UnionWith(selectedIds); rectanglePrimary = selected?.node.id;
            rectangleBase.Clear(); rectangleBase.UnionWith(rectangleSaved);
            if (!evt.shiftKey && !evt.actionKey) rectangleBase.Clear();
            rectangleStart = (pointer - pan) / zoom; selectingRectangle = true;
            SelectRectangle(new Rect(rectangleStart, Vector2.zero), rectangleBase);
            Capture(evt.pointerId); evt.StopPropagation();
        }
    }
    private void Move(PointerMoveEvent evt)
    {
        Vector2 next = this.WorldToLocal(evt.position), delta = next - pointer; pointer = next;
        if (panning) { pan += delta; TransformContent(); }
        else if (dragging) MoveSelectedNodes(delta / zoom);
        else if (selectingRectangle) SelectRectangle(SelectionRectangle(), rectangleBase);
        else if (linking != null) MarkDirtyRepaint();
    }
    private void Up(PointerUpEvent evt)
    {
        if (linking != null) foreach (var target in ports)
            if (target != linking && target.worldBound.Contains(evt.position)) { ConnectPorts(linking, target); break; }
        Release();
    }
    private void Wheel(WheelEvent evt)
    {
        Vector2 local = this.WorldToLocal(evt.mousePosition), anchor = (local - pan) / zoom;
        zoom = Mathf.Clamp(zoom * Mathf.Exp(-evt.delta.y * .06f), .1f, 2); pan = local - anchor * zoom;
        TransformContent(); evt.StopPropagation();
    }
    private void Key(KeyDownEvent evt)
    {
        var element = evt.target as VisualElement;
        if (element is BaseField<int> || element?.GetFirstAncestorOfType<BaseField<int>>() != null) return;
        if (evt.keyCode == KeyCode.Escape)
        {
            if (selectingRectangle)
            {
                selectedIds.Clear(); selectedIds.UnionWith(rectangleSaved);
                selected = cards.Find(card => card.node.id == rectanglePrimary && IsSelected(card.node));
                NotifySelection();
            }
            Release(); evt.StopPropagation();
        }
        else if (evt.actionKey && evt.keyCode == KeyCode.A) { SelectAllNodes(); evt.StopPropagation(); }
        else if (evt.actionKey && evt.keyCode == KeyCode.C) { CopySelectedNodes(); evt.StopPropagation(); }
        else if (evt.actionKey && evt.keyCode == KeyCode.V) { PasteNodes(EditorGUIUtility.systemCopyBuffer); evt.StopPropagation(); }
        else if (evt.actionKey && evt.keyCode == KeyCode.D) { DuplicateSelectedNodes(); evt.StopPropagation(); }
        else if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) { RemoveSelectedNodes(); evt.StopPropagation(); }
        else if (evt.keyCode == KeyCode.F) { FrameAll(); evt.StopPropagation(); }
    }
    private bool CanHandleCommand(string command, VisualElement element)
    {
        if (graph == null || element is BaseField<int> || element?.GetFirstAncestorOfType<BaseField<int>>() != null) return false;
        if (command == "Copy") return SelectionCount > 0;
        if (command == "SelectAll") return cards.Count > 0;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        if (command == "Paste") return MergeRecipeGraphClipboard.CanPaste(EditorGUIUtility.systemCopyBuffer);
        return SelectionCount > 0 && (command == "Duplicate" || command == "Delete" || command == "SoftDelete");
    }
    private void UpdateSelection()
    {
        foreach (var card in cards)
        {
            Color color = IsSelected(card.node) ? new Color(.25f, .8f, 1) : new Color(.35f, .38f, .42f);
            card.style.borderLeftColor = card.style.borderRightColor = card.style.borderTopColor = card.style.borderBottomColor = color;
        }
    }
    private void NotifySelection()
    {
        if (selected == null || !IsSelected(selected.node)) selected = cards.Find(card => IsSelected(card.node));
        UpdateSelection(); select(PrimarySelection); MarkDirtyRepaint();
    }
    internal void SelectNode(MergeGraphItemNode node, bool additive = false, bool keepSelectionIfSelected = false)
    {
        var card = cards.Find(value => value.node.id == node?.id);
        if (card == null) return;
        if (additive)
        { if (!selectedIds.Add(node.id)) selectedIds.Remove(node.id); }
        else if (!keepSelectionIfSelected || !IsSelected(node))
        { selectedIds.Clear(); selectedIds.Add(node.id); }
        if (IsSelected(node)) selected = card;
        NotifySelection();
    }
    internal void SelectAllNodes()
    {
        foreach (var card in cards) selectedIds.Add(card.node.id);
        NotifySelection();
    }
    private Rect SelectionRectangle()
    {
        Vector2 end = (pointer - pan) / zoom;
        return Rect.MinMaxRect(Mathf.Min(rectangleStart.x, end.x), Mathf.Min(rectangleStart.y, end.y),
            Mathf.Max(rectangleStart.x, end.x), Mathf.Max(rectangleStart.y, end.y));
    }
    internal void SelectRectangle(Rect area, IEnumerable<string> additiveSelection = null)
    {
        selectedIds.Clear();
        if (additiveSelection != null) selectedIds.UnionWith(additiveSelection);
        if (area.width > 0 && area.height > 0)
            foreach (var card in cards)
                if (area.Overlaps(new Rect(card.node.position, new Vector2(240, card.Height)))) selectedIds.Add(card.node.id);
        NotifySelection();
    }
    internal void MoveSelectedNodes(Vector2 delta)
    {
        if (graph == null || SelectionCount == 0 || delta == Vector2.zero || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Undo.RecordObject(graph, "Move selected merge nodes");
        foreach (var card in cards) if (IsSelected(card.node))
        {
            card.node.position += delta;
            card.style.left = card.node.position.x; card.style.top = card.node.position.y;
        }
        EditorUtility.SetDirty(graph); MarkDirtyRepaint();
    }
    internal void RemoveSelectedNodes()
    {
        if (graph == null || SelectionCount == 0 || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Release(); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        foreach (var card in cards) if (IsSelected(card.node)) MergeRecipeGraphEditing.RemoveItem(graph, card.node);
        Undo.CollapseUndoOperations(group); Rebuild(null);
    }
    private string SelectedClipboard()
        => MergeRecipeGraphClipboard.Copy(graph, cards.FindAll(card => IsSelected(card.node)).ConvertAll(card => card.node));
    internal void CopySelectedNodes()
    {
        if (SelectionCount > 0) EditorGUIUtility.systemCopyBuffer = SelectedClipboard();
    }
    internal void DuplicateSelectedNodes() => PasteNodes(SelectedClipboard());
    internal void PasteNodes(string clipboard)
    {
        if (graph == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Release();
        var added = MergeRecipeGraphClipboard.Paste(graph, clipboard);
        if (added.Count == 0) return;
        selectedIds.Clear(); foreach (var node in added) selectedIds.Add(node.id);
        Rebuild(added[added.Count - 1]); status(added.Count + "개 노드를 붙여넣었습니다.");
    }
    internal void ConnectPorts(VisualElement source, VisualElement target)
    {
        if (graph == null || EditorApplication.isPlaying || !(source is Port a) || !(target is Port b)) return;
        var output = a.slot < 0 ? a : b.slot < 0 ? b : null;
        var input = a.slot >= 0 ? a : b.slot >= 0 ? b : null;
        if (output == null || input == null) { status("머지 아이템 출력 → 결과 아이템의 재료 입력을 연결하세요."); return; }
        MergeRecipeGraphEditing.SetInput(graph, input.card.node, input.slot, output.card.node.item, output.card.node.id);
        RefreshPreviews(); status("연결했습니다. 레시피 추출 → 티어별 레시피북 생성으로 반영하세요.");
    }
    private void Disconnect(Port port)
    {
        if (graph == null || EditorApplication.isPlaying) return;
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        if (port.slot >= 0) MergeRecipeGraphEditing.SetInput(graph, port.card.node, port.slot, null);
        else foreach (var node in graph.items) for (int i = 0; i < node.ingredients.Count; i++)
            if (MergeRecipeGraphEditing.InputNode(graph, node, i) == port.card.node) MergeRecipeGraphEditing.SetInput(graph, node, i, null);
        Undo.CollapseUndoOperations(group); RefreshPreviews();
    }
    public void FrameAll()
    {
        if (float.IsNaN(contentRect.width) || float.IsNaN(contentRect.height) || contentRect.width < 1 || contentRect.height < 1) return;
        if (cards.Count == 0) { pan = new Vector2(40, 40); zoom = 1; TransformContent(); return; }
        Vector2 min = cards[0].node.position, max = min + new Vector2(240, cards[0].Height);
        foreach (var card in cards) { min = Vector2.Min(min, card.node.position); max = Vector2.Max(max, card.node.position + new Vector2(240, card.Height)); }
        Vector2 size = max - min + new Vector2(100, 100);
        zoom = Mathf.Clamp(Mathf.Min(contentRect.width / size.x, contentRect.height / size.y), .1f, 1.4f);
        pan = contentRect.size * .5f - (min + max) * .5f * zoom; TransformContent();
    }
    private Port Output(MergeGraphItemNode node) => ports.Find(port => port.slot < 0 && port.card.node == node);
    private Port Input(MergeGraphItemNode node, int slot) => ports.Find(port => port.card.node == node && port.slot == slot);
    private static void Curve(Painter2D painter, Vector2 start, Vector2 end)
    {
        float tangent = Mathf.Max(45, Mathf.Abs(end.x - start.x) * .45f);
        painter.strokeColor = new Color(.35f, .75f, 1); painter.lineWidth = 2;
        painter.BeginPath(); painter.MoveTo(start); painter.BezierCurveTo(start + Vector2.right * tangent, end + Vector2.left * tangent, end); painter.Stroke();
    }
    private void Paint(MeshGenerationContext context)
    {
        var painter = context.painter2D; float spacing = Mathf.Max(12, 24 * zoom);
        painter.strokeColor = new Color(.19f, .2f, .23f); painter.lineWidth = 1; painter.BeginPath();
        for (float x = pan.x % spacing; x < contentRect.width; x += spacing) { painter.MoveTo(new Vector2(x, 0)); painter.LineTo(new Vector2(x, contentRect.height)); }
        for (float y = pan.y % spacing; y < contentRect.height; y += spacing) { painter.MoveTo(new Vector2(0, y)); painter.LineTo(new Vector2(contentRect.width, y)); }
        painter.Stroke();
        if (graph != null) foreach (var node in graph.items) for (int i = 0; i < node.ingredients.Count; i++)
        {
            var source = MergeRecipeGraphEditing.InputNode(graph, node, i); var output = Output(source); var input = Input(node, i);
            if (source != null && output != null && input != null) Curve(painter, Point(output), Point(input));
        }
        if (linking != null) Curve(painter, linking.slot < 0 ? Point(linking) : pointer, linking.slot < 0 ? pointer : Point(linking));
        if (selectingRectangle)
        {
            var rect = SelectionRectangle(); Vector2 min = rect.min * zoom + pan, max = rect.max * zoom + pan;
            painter.fillColor = new Color(.25f, .8f, 1, .12f); painter.strokeColor = new Color(.25f, .8f, 1); painter.lineWidth = 1;
            painter.BeginPath(); painter.MoveTo(min); painter.LineTo(new Vector2(max.x, min.y));
            painter.LineTo(max); painter.LineTo(new Vector2(min.x, max.y)); painter.ClosePath(); painter.Fill(); painter.Stroke();
        }
    }
}
