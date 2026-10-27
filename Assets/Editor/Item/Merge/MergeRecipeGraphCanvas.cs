using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

internal sealed class MergeRecipeGraphCanvas : VisualElement
{
    private enum PortKind { ItemInput, ItemOutput, IngredientA, IngredientB, Result }
    private sealed class Port : VisualElement
    {
        public PortKind kind;
        public Card card;
    }
    private sealed class Card : VisualElement
    {
        public MergeGraphItemNode item;
        public MergeGraphRecipeNode recipe;
        public Label title, detail;
        public Image icon;
        public Object Asset => item != null ? (Object)item.item : recipe.recipe;
        public Vector2 Position
        {
            get => item != null ? item.position : recipe.position;
            set { if (item != null) item.position = value; else recipe.position = value; }
        }
    }

    private readonly VisualElement content = new VisualElement();
    private readonly List<Card> cards = new List<Card>();
    private readonly List<Port> ports = new List<Port>();
    private readonly Action<Object> select;
    private readonly Action<Vector2> createItem, createRecipe;
    private readonly Action<Object[]> import;
    private readonly Action<string> status;
    private MergeRecipeGraphAsset graph;
    private Card selected, dragged;
    private Port linking;
    private Vector2 pan = new Vector2(40, 40), pointer;
    private float zoom = 1f;
    private bool panning;
    private int capturedPointer;
    private int dragUndoGroup = -1;
    public Vector2 Center => (contentRect.size * .5f - pan) / zoom;

    public MergeRecipeGraphCanvas(Action<Object> select, Action<Vector2> createItem, Action<Vector2> createRecipe,
        Action<Object[]> import, Action<string> status)
    {
        this.select = select; this.createItem = createItem; this.createRecipe = createRecipe;
        this.import = import; this.status = status;
        style.flexGrow = 1;
        style.minWidth = 300;
        style.overflow = Overflow.Hidden;
        style.backgroundColor = new Color(.105f, .115f, .13f);
        focusable = true;
        content.style.position = UnityEngine.UIElements.Position.Absolute;
        content.style.width = content.style.height = 100000;
        content.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0), 0);
        Add(content);
        generateVisualContent += Paint;
        RegisterCallback<PointerDownEvent>(Down);
        RegisterCallback<PointerMoveEvent>(Move);
        RegisterCallback<PointerUpEvent>(Up);
        RegisterCallback<PointerCaptureOutEvent>(_ => FinishGesture());
        RegisterCallback<WheelEvent>(Wheel);
        RegisterCallback<KeyDownEvent>(Key);
        RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var element = evt.target as VisualElement;
            if (element is Card || element?.GetFirstAncestorOfType<Card>() != null) return;
            Vector2 at = ToGraph(evt.mousePosition);
            evt.menu.AppendAction("+ 머지 아이템", _ => createItem(at));
            evt.menu.AppendAction("+ 레시피", _ => createRecipe(at));
        }));
        RegisterCallback<DragUpdatedEvent>(evt =>
        {
            foreach (var asset in DragAndDrop.objectReferences)
                if (asset is MergeItem || asset is ItemRecipeData)
                { DragAndDrop.visualMode = DragAndDropVisualMode.Link; evt.StopPropagation(); break; }
        });
        RegisterCallback<DragPerformEvent>(evt =>
        {
            DragAndDrop.AcceptDrag(); import(DragAndDrop.objectReferences); evt.StopPropagation();
        });
        TransformContent();
    }

    public void SetGraph(MergeRecipeGraphAsset value, Object selection)
    {
        graph = value;
        content.Clear(); cards.Clear(); ports.Clear();
        selected = dragged = null; linking = null;
        if (graph != null)
        {
            foreach (var node in graph.items) AddCard(new Card { item = node });
            foreach (var node in graph.recipes) AddCard(new Card { recipe = node });
            foreach (var card in cards) if (card.Asset == selection && selection != null) selected = card;
        }
        UpdateSelection();
        RefreshPreviews();
        MarkDirtyRepaint();
    }

    private static void Place(VisualElement element, float x, float y, float width, float height)
    {
        element.style.position = UnityEngine.UIElements.Position.Absolute;
        element.style.left = x; element.style.top = y;
        element.style.width = width; element.style.height = height;
    }

    private void AddCard(Card card)
    {
        card.name = card.item != null ? "merge-item-node" : "merge-recipe-node";
        Place(card, card.Position.x, card.Position.y, 220, 140);
        card.style.backgroundColor = card.item != null ? new Color(.19f, .24f, .29f) : new Color(.28f, .23f, .16f);
        card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
        card.style.borderLeftWidth = card.style.borderRightWidth = card.style.borderTopWidth = card.style.borderBottomWidth = 2;
        card.title = new Label { pickingMode = PickingMode.Ignore };
        Place(card.title, 12, 8, 196, 30);
        card.title.style.unityFontStyleAndWeight = FontStyle.Bold;
        card.title.style.whiteSpace = WhiteSpace.Normal;
        card.Add(card.title);
        card.detail = new Label { pickingMode = PickingMode.Ignore };
        card.detail.style.whiteSpace = WhiteSpace.Normal;
        card.detail.style.fontSize = 11;
        if (card.item != null)
        {
            card.icon = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            Place(card.icon, 16, 48, 48, 48); card.Add(card.icon);
            Place(card.detail, 76, 46, 130, 60);
            AddPort(card, PortKind.ItemInput, -7, 115, "결과 입력");
            AddPort(card, PortKind.ItemOutput, 213, 115, "재료 출력");
            LabelAt(card, "결과", 14, 112); LabelAt(card, "재료", 165, 112);
        }
        else
        {
            Place(card.detail, 12, 108, 192, 24);
            AddPort(card, PortKind.IngredientA, -7, 50, "재료 A"); LabelAt(card, "재료 A", 14, 47);
            AddPort(card, PortKind.IngredientB, -7, 82, "재료 B"); LabelAt(card, "재료 B", 14, 79);
            AddPort(card, PortKind.Result, 213, 66, "결과 출력"); LabelAt(card, "결과", 165, 63);
        }
        card.Add(card.detail);
        card.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0 || evt.altKey || graph == null || EditorApplication.isPlaying) return;
            Focus();
            selected = card; select(card.Asset); UpdateSelection();
            Undo.IncrementCurrentGroup(); dragUndoGroup = Undo.GetCurrentGroup();
            dragged = card; Capture(evt.pointerId);
            evt.StopPropagation();
        });
        card.RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        card.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Project에서 보기", _ => { if (card.Asset != null) EditorGUIUtility.PingObject(card.Asset); });
            evt.menu.AppendAction("그래프에서 제거 (에셋 유지)", _ => Remove(card));
            evt.StopPropagation();
        }));
        cards.Add(card); content.Add(card);
    }

    private static void LabelAt(Card card, string text, float x, float y)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.style.fontSize = 11;
        Place(label, x, y, 55, 20);
        card.Add(label);
    }

    private void AddPort(Card card, PortKind kind, float x, float y, string tooltip)
    {
        var port = new Port { name = kind.ToString(), card = card, kind = kind, tooltip = tooltip + " · 드래그로 연결 / 우클릭으로 해제" };
        Place(port, x, y, 14, 14);
        port.style.borderTopLeftRadius = port.style.borderTopRightRadius = port.style.borderBottomLeftRadius = port.style.borderBottomRightRadius = 7;
        port.style.backgroundColor = kind == PortKind.Result || kind == PortKind.ItemInput
            ? new Color(.5f, .85f, .45f) : new Color(.35f, .75f, 1f);
        port.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0 || graph == null || EditorApplication.isPlaying) return;
            Focus(); linking = port; pointer = this.WorldToLocal(evt.position); Capture(evt.pointerId);
            evt.StopPropagation(); MarkDirtyRepaint();
        });
        port.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("연결 해제", _ => Disconnect(port)); evt.StopPropagation();
        }));
        ports.Add(port); card.Add(port);
    }

    public void RefreshPreviews()
    {
        foreach (var card in cards)
        {
            if (card.item != null)
            {
                var definition = card.item.item;
                var data = definition != null ? definition.item : null;
                card.title.text = definition != null ? definition.name : "MergeItem 없음";
                card.icon.sprite = data != null ? data.icon : null;
                card.detail.text = data != null ? $"{data.name}\n{data.series} · {data.grade}\n반지름 {definition.Radius:0.##}" : "ItemData를 연결하세요";
            }
            else
            {
                var recipe = card.recipe.recipe;
                card.title.text = recipe != null ? $"T{recipe.tier} · {recipe.name}" : "레시피 없음";
                bool doubled = MergeRecipeGraphEditing.TryInputs(recipe, out var first, out var second) && first != null && first == second;
                card.detail.text = (doubled ? "×2 " : "") +
                    (recipe != null && recipe.resultItem != null ? "→ " + recipe.resultItem.name : "결과를 연결하세요");
            }
        }
        MarkDirtyRepaint();
    }

    private Vector2 ToGraph(Vector2 panelPoint) => (this.WorldToLocal(panelPoint) - pan) / zoom;
    private Vector2 PortPoint(Port port) => this.WorldToLocal(port.worldBound.center);
    private void TransformContent()
    {
        content.style.translate = new Translate(pan.x, pan.y);
        content.style.scale = new Scale(new Vector3(zoom, zoom, 1));
        MarkDirtyRepaint();
    }
    private void Capture(int id) { capturedPointer = id; this.CapturePointer(id); }
    private void Release()
    {
        FinishGesture();
        if (this.HasPointerCapture(capturedPointer)) this.ReleasePointer(capturedPointer);
    }
    private void FinishGesture()
    {
        if (dragUndoGroup >= 0) Undo.CollapseUndoOperations(dragUndoGroup);
        dragUndoGroup = -1;
        linking = null; dragged = null; panning = false; MarkDirtyRepaint();
    }
    private void Down(PointerDownEvent evt)
    {
        Focus();
        if (evt.button == 2 || (evt.button == 0 && evt.altKey)) { panning = true; Capture(evt.pointerId); evt.StopPropagation(); }
        else if (evt.button == 0) { selected = null; UpdateSelection(); select(null); }
    }
    private void Move(PointerMoveEvent evt)
    {
        pointer = this.WorldToLocal(evt.position);
        if (panning) { pan += (Vector2)evt.deltaPosition; TransformContent(); }
        else if (dragged != null)
        {
            Undo.RecordObject(graph, "Move merge graph node");
            dragged.Position += (Vector2)evt.deltaPosition / zoom;
            dragged.style.left = dragged.Position.x; dragged.style.top = dragged.Position.y;
            EditorUtility.SetDirty(graph); MarkDirtyRepaint();
        }
        else if (linking != null) MarkDirtyRepaint();
    }
    private void Up(PointerUpEvent evt)
    {
        if (linking != null)
            foreach (var target in ports)
                if (target != linking && target.worldBound.Contains(evt.position)) { ConnectPorts(linking, target); break; }
        Release();
    }
    private void Wheel(WheelEvent evt)
    {
        Vector2 local = this.WorldToLocal(evt.mousePosition);
        Vector2 anchor = (local - pan) / zoom;
        zoom = Mathf.Clamp(zoom * Mathf.Exp(-evt.delta.y * .06f), .1f, 2f);
        pan = local - anchor * zoom;
        TransformContent(); evt.StopPropagation();
    }
    private void Key(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape) Release();
        else if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
        { if (selected != null) Remove(selected); evt.StopPropagation(); }
        else if (evt.keyCode == KeyCode.F) { FrameAll(); evt.StopPropagation(); }
    }
    private void UpdateSelection()
    {
        foreach (var card in cards)
        {
            Color color = card == selected ? new Color(.25f, .8f, 1f) : new Color(.35f, .38f, .42f);
            card.style.borderLeftColor = card.style.borderRightColor = card.style.borderTopColor = card.style.borderBottomColor = color;
        }
    }
    private void Remove(Card card)
    {
        if (graph == null || EditorApplication.isPlaying) return;
        Undo.RecordObject(graph, "Remove merge graph node");
        if (card.item != null) graph.items.Remove(card.item); else graph.recipes.Remove(card.recipe);
        EditorUtility.SetDirty(graph);
        SetGraph(graph, null); select(null);
    }

    internal void ConnectPorts(VisualElement source, VisualElement target)
    {
        if (graph == null || EditorApplication.isPlaying) return;
        if (source is Port first && target is Port second) Connect(first, second);
    }

    private void Connect(Port a, Port b)
    {
        var itemOutput = a.kind == PortKind.ItemOutput ? a : b.kind == PortKind.ItemOutput ? b : null;
        var ingredient = a.kind == PortKind.IngredientA || a.kind == PortKind.IngredientB ? a :
            b.kind == PortKind.IngredientA || b.kind == PortKind.IngredientB ? b : null;
        var result = a.kind == PortKind.Result ? a : b.kind == PortKind.Result ? b : null;
        var itemInput = a.kind == PortKind.ItemInput ? a : b.kind == PortKind.ItemInput ? b : null;
        try
        {
            if (itemOutput != null && ingredient != null)
            {
                var data = itemOutput.card.item.item != null ? itemOutput.card.item.item.item : null;
                if (data == null) throw new InvalidOperationException("아이템 노드에 ItemData를 먼저 연결하세요.");
                MergeRecipeGraphEditing.SetInput(ingredient.card.recipe.recipe, ingredient.kind == PortKind.IngredientA ? 0 : 1, data);
            }
            else if (result != null && itemInput != null)
            {
                var data = itemInput.card.item.item != null ? itemInput.card.item.item.item : null;
                if (data == null) throw new InvalidOperationException("결과 아이템에 ItemData를 먼저 연결하세요.");
                MergeRecipeGraphEditing.SetResult(result.card.recipe.recipe, data);
            }
            else throw new InvalidOperationException("아이템 재료 출력 → 레시피 A/B, 레시피 결과 → 아이템 결과 입력을 연결하세요.");
            RefreshPreviews(); status("레시피 연결을 변경했습니다. 저장하면 기존 ItemRecipeData에 반영됩니다.");
        }
        catch (Exception exception) { status(exception.Message); }
    }

    private void Disconnect(Port port)
    {
        if (EditorApplication.isPlaying || graph == null) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        if (port.card.recipe != null)
        {
            var recipe = port.card.recipe.recipe;
            if (recipe == null) return;
            if (port.kind == PortKind.Result) MergeRecipeGraphEditing.SetResult(recipe, null);
            else MergeRecipeGraphEditing.SetInput(recipe, port.kind == PortKind.IngredientA ? 0 : 1, null);
        }
        else if (port.card.item.item != null && port.card.item.item.item != null)
        {
            var data = port.card.item.item.item;
            foreach (var node in graph.recipes)
            {
                if (node.recipe == null) continue;
                if (port.kind == PortKind.ItemInput)
                { if (node.recipe.resultItem == data) MergeRecipeGraphEditing.SetResult(node.recipe, null); }
                else if (MergeRecipeGraphEditing.TryInputs(node.recipe, out var first, out var second))
                {
                    if (first == data) MergeRecipeGraphEditing.SetInput(node.recipe, 0, null);
                    if (second == data) MergeRecipeGraphEditing.SetInput(node.recipe, 1, null);
                }
            }
        }
        Undo.CollapseUndoOperations(group);
        RefreshPreviews();
    }

    public void FrameAll()
    {
        if (float.IsNaN(contentRect.width) || float.IsNaN(contentRect.height) || contentRect.width < 1 || contentRect.height < 1) return;
        if (cards.Count == 0) { pan = new Vector2(40, 40); zoom = 1; TransformContent(); return; }
        Vector2 min = cards[0].Position, max = min + new Vector2(220, 140);
        foreach (var card in cards) { min = Vector2.Min(min, card.Position); max = Vector2.Max(max, card.Position + new Vector2(220, 140)); }
        Vector2 size = max - min + new Vector2(100, 100);
        zoom = Mathf.Clamp(Mathf.Min(contentRect.width / size.x, contentRect.height / size.y), .1f, 1.4f);
        pan = contentRect.size * .5f - (min + max) * .5f * zoom;
        TransformContent();
    }

    private Port FindPort(ItemData data, PortKind kind)
    {
        if (data == null) return null;
        foreach (var port in ports)
            if (port.kind == kind && port.card.item?.item != null && port.card.item.item.item == data) return port;
        return null;
    }
    private Port FindPort(MergeGraphRecipeNode node, PortKind kind)
    {
        foreach (var port in ports) if (port.kind == kind && port.card.recipe == node) return port;
        return null;
    }
    private static void Curve(Painter2D painter, Vector2 start, Vector2 end, Color color)
    {
        float tangent = Mathf.Max(45, Mathf.Abs(end.x - start.x) * .45f);
        painter.strokeColor = color; painter.lineWidth = 2;
        painter.BeginPath(); painter.MoveTo(start);
        painter.BezierCurveTo(start + Vector2.right * tangent, end + Vector2.left * tangent, end);
        painter.Stroke();
    }
    private void Line(Painter2D painter, Port from, Port to, Color color)
    { if (from != null && to != null) Curve(painter, PortPoint(from), PortPoint(to), color); }
    private void Paint(MeshGenerationContext context)
    {
        var painter = context.painter2D;
        float spacing = Mathf.Max(12, 24 * zoom);
        painter.strokeColor = new Color(.19f, .2f, .23f); painter.lineWidth = 1;
        painter.BeginPath();
        for (float x = pan.x % spacing; x < contentRect.width; x += spacing)
        { painter.MoveTo(new Vector2(x, 0)); painter.LineTo(new Vector2(x, contentRect.height)); }
        for (float y = pan.y % spacing; y < contentRect.height; y += spacing)
        { painter.MoveTo(new Vector2(0, y)); painter.LineTo(new Vector2(contentRect.width, y)); }
        painter.Stroke();
        if (graph != null) foreach (var node in graph.recipes)
        {
            if (!MergeRecipeGraphEditing.TryInputs(node.recipe, out var first, out var second)) continue;
            Line(painter, FindPort(first, PortKind.ItemOutput), FindPort(node, PortKind.IngredientA), new Color(.35f, .75f, 1));
            Line(painter, FindPort(second, PortKind.ItemOutput), FindPort(node, PortKind.IngredientB), new Color(.35f, .75f, 1));
            Line(painter, FindPort(node, PortKind.Result), FindPort(node.recipe.resultItem, PortKind.ItemInput), new Color(.5f, .85f, .45f));
        }
        if (linking != null)
        {
            bool output = linking.kind == PortKind.ItemOutput || linking.kind == PortKind.Result;
            Curve(painter, output ? PortPoint(linking) : pointer, output ? pointer : PortPoint(linking), Color.white);
        }
    }
}
