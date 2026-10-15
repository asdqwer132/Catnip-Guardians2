using System.Collections.Generic;
using UnityEngine;

// 여러 시리즈가 하나의 TokenView 풀을 공유하며, 전투와 분리한 PhysicsScene2D에서만 충돌합니다.
public sealed class MergeContainerRuntime
{
    public readonly MergeContainerDefinition Definition;
    public readonly MergeContainerBoard Board;
    public readonly Transform Root;
    public MergeContainerStats Stats { get; private set; }
    public readonly Dictionary<int, MergeContainerTokenView> Views = new Dictionary<int, MergeContainerTokenView>();
    public bool InletBlocked { get; private set; }
    private readonly MergeGameSceneController host;
    private readonly Transform[] walls = new Transform[3];
    private float injectionTimer;

    public MergeContainerRuntime(MergeGameSceneController host, MergeContainerDefinition definition, Transform root)
    {
        this.host = host; Definition = definition; Root = root; Board = new MergeContainerBoard(definition);
        for (int i = 0; i < walls.Length; i++)
        {
            var wall = new GameObject("Container Wall"); wall.layer = host.ContainerLayer;
            wall.transform.SetParent(root, false); wall.AddComponent<BoxCollider2D>();
            var visual = wall.AddComponent<SpriteRenderer>(); visual.sprite = host.SquareSprite;
            visual.sharedMaterial = host.SpriteMaterial; visual.color = definition.color;
            walls[i] = wall.transform;
        }
        RefreshStats();
    }
    public void RefreshStats()
    {
        Stats = MergeGameSession.Ensure().upgrades.Resolve(Definition);
        walls[0].localPosition = new Vector3(0f, -0.15f); walls[0].localScale = new Vector3(Stats.width + 0.6f, 0.3f, 1f);
        walls[1].localPosition = new Vector3(-Stats.width * 0.5f - 0.15f, Stats.height * 0.5f);
        walls[2].localPosition = new Vector3(Stats.width * 0.5f + 0.15f, Stats.height * 0.5f);
        walls[1].localScale = walls[2].localScale = new Vector3(0.3f, Stats.height + 0.3f, 1f);
    }
    public void Tick(float delta, bool inject)
    {
        Board.Tick(delta);
        // 상단이 열린 통에서도 큰 합성 결과나 물리 보정 때문에 아이템이 벽 밖으로 유실되지 않게 합니다.
        foreach (var view in Views.Values)
        {
            Vector3 local = view.transform.position - Root.position;
            Vector3 safe = new Vector3(Mathf.Clamp(local.x, -Stats.width * 0.5f + view.Radius, Stats.width * 0.5f - view.Radius),
                Mathf.Clamp(local.y, view.Radius, Stats.height - view.Radius), 0f);
            if ((local - safe).sqrMagnitude > 0.0001f) { view.transform.position = Root.position + safe; view.Body.velocity = Vector2.zero; }
        }
        if (!inject) return;
        injectionTimer += delta;
        if (injectionTimer < Stats.injectionInterval) return;
        injectionTimer = 0f; InletBlocked = false;
        if (Definition.injectionMaterials == null || Board.Count >= Stats.maximumItems) { InletBlocked = true; return; }
        for (int inlet = 0; inlet < Stats.inletCount; inlet++)
        {
            var candidates = new List<ItemData>();
            foreach (var item in Definition.injectionMaterials)
                if (Definition.CanInject(item) && host.Inventory != null && host.Inventory.HasItem(item, 1)) candidates.Add(item);
            if (candidates.Count == 0) return;
            var material = candidates[Random.Range(0, candidates.Count)];
            var entry = Definition.Find(material);
            float radius = Mathf.Clamp(entry.radius, 0.08f, Mathf.Min(Stats.width, Stats.height) * 0.2f);
            float horizontal = (inlet + 1f) / (Stats.inletCount + 1f);
            Vector3 point = Root.position + new Vector3(Mathf.Lerp(-Stats.width * 0.5f + radius, Stats.width * 0.5f - radius, horizontal), Stats.height - radius - 0.05f);
            bool occupied = false;
            foreach (var view in Views.Values)
                if (((Vector2)(view.transform.position - point)).sqrMagnitude < Mathf.Pow(view.Radius + radius + 0.05f, 2f)) { occupied = true; break; }
            if (occupied || Board.Count >= Stats.maximumItems) { InletBlocked = true; continue; }
            // 빈 공간이 확인된 뒤에만 보유 재료를 소비합니다.
            if (!MergeContainerSupply.TryInject(Board, material, Stats.maximumItems, true, host.Catalog.supplyMode,
                item => host.Inventory.HasItem(item, 1), item => host.Inventory.RemoveItem(item, 1),
                item => host.Inventory.AddItem(item, 1), out int id)) continue;
            SpawnView(id, point, Vector2.zero);
        }
    }
    private void SpawnView(int id, Vector3 point, Vector2 velocity)
    {
        var token = Board.Get(id); if (token == null) return;
        var view = host.TakeView(); view.transform.SetParent(Root, false); view.transform.position = point;
        view.Bind(this, id, Definition.Find(token.item), host.CircleSprite, host.SpriteMaterial);
        view.Body.velocity = velocity; Views.Add(id, view);
    }
    public bool TryMerge(MergeContainerTokenView first, MergeContainerTokenView second)
    {
        if (first == null || second == null || first.Container != this || second.Container != this) return false;
        Vector3 point = (first.transform.position + second.transform.position) * 0.5f;
        Vector2 velocity = (first.Body.velocity + second.Body.velocity) * 0.5f;
        int a = first.TokenId, b = second.TokenId;
        if (!Board.TryMerge(a, b, out int id)) return false;
        ReturnView(a); ReturnView(b);
        float radius = Mathf.Clamp(Definition.Find(Board.Get(id).item).radius, 0.08f, Mathf.Min(Stats.width, Stats.height) * 0.2f);
        point.x = Mathf.Clamp(point.x, Root.position.x - Stats.width * 0.5f + radius, Root.position.x + Stats.width * 0.5f - radius);
        point.y = Mathf.Max(point.y, Root.position.y + radius);
        SpawnView(id, point, Vector2.ClampMagnitude(velocity, 5f)); return true;
    }
    public void ReturnView(int id)
    { if (Views.TryGetValue(id, out var view)) { Views.Remove(id); host.ReturnView(view); } }
    public void Clear()
    { foreach (int id in new List<int>(Views.Keys)) ReturnView(id); Board.Clear(); injectionTimer = 0f; InletBlocked = false; }
    public void RefreshSelection()
    { foreach (var pair in Views) pair.Value.SetSelected(Board.Get(pair.Key)?.selected == true); }
    public void SelectNearSegment(Vector3 from, Vector3 to, float radius)
    {
        Vector2 segment = to - from; float length = segment.sqrMagnitude;
        foreach (var pair in Views)
        {
            Vector2 offset = pair.Value.transform.position - from;
            float t = length <= 0.000001f ? 0f : Mathf.Clamp01(Vector2.Dot(offset, segment) / length);
            if ((offset - segment * t).sqrMagnitude <= Mathf.Pow(radius + pair.Value.Radius, 2f)) Board.Select(pair.Key);
        }
        RefreshSelection();
    }
    public void SelectOne(Vector3 point)
    {
        int selected = 0; float best = float.MaxValue;
        foreach (var pair in Views)
        {
            float distance = ((Vector2)(pair.Value.transform.position - point)).sqrMagnitude;
            if (distance <= pair.Value.Radius * pair.Value.Radius && distance < best) { selected = pair.Key; best = distance; }
        }
        if (selected != 0) Board.Select(selected);
        RefreshSelection();
    }
}
