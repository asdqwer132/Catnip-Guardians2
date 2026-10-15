using System.Collections.Generic;

// 물리/화면과 분리한 상태입니다. 선택한 두 아이템은 합성할 수 없고, 투척 성공분만 제거합니다.
public sealed class MergeContainerToken
{
    public int id;
    public ItemData item;
    public bool selected;
}

public sealed class MergeContainerBoard
{
    private readonly Dictionary<int, MergeContainerToken> tokens = new Dictionary<int, MergeContainerToken>();
    private int nextId;
    private float clickRemaining, dragRemaining;
    public MergeContainerDefinition Definition { get; }
    public IEnumerable<MergeContainerToken> Tokens => tokens.Values;
    public int Count => tokens.Count;
    public MergeContainerBoard(MergeContainerDefinition definition) { Definition = definition; }
    public MergeContainerToken Get(int id) => tokens.TryGetValue(id, out var token) ? token : null;
    public int Add(ItemData item, int maximum)
    {
        if (Definition == null || Definition.Find(item) == null || item.series != Definition.series || Count >= maximum) return 0;
        var token = new MergeContainerToken { id = ++nextId, item = item };
        tokens.Add(token.id, token); return token.id;
    }
    public bool TryMerge(int first, int second, out int result)
    {
        result = 0;
        var a = Get(first); var b = Get(second);
        if (a == null || b == null || first == second || a.selected || b.selected ||
            !Definition.TryGetMergeResult(a.item, b.item, out var item)) return false;
        tokens.Remove(first); tokens.Remove(second);
        result = Add(item, int.MaxValue); return result != 0;
    }
    public bool Select(int id)
    { var token = Get(id); if (token == null) return false; token.selected = true; return true; }
    public void ClearSelection() { foreach (var token in tokens.Values) token.selected = false; }
    public List<int> SelectedIds()
    {
        var result = new List<int>();
        foreach (var token in tokens.Values) if (token.selected) result.Add(token.id);
        result.Sort(); return result;
    }
    public bool CommitThrow(int id, bool succeeded)
    { var token = Get(id); if (!succeeded || token == null || !token.selected) return false; return tokens.Remove(id); }
    public float Cooldown(bool drag) => drag ? dragRemaining : clickRemaining;
    public bool CanSelect(bool drag) => Cooldown(drag) <= 0f;
    public void SpendCooldown(bool drag, float duration)
    {
        if (float.IsNaN(duration) || float.IsInfinity(duration)) duration = 0f;
        if (drag) dragRemaining = System.Math.Max(0f, duration); else clickRemaining = System.Math.Max(0f, duration);
    }
    public void Tick(float delta)
    {
        if (delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        clickRemaining = System.Math.Max(0f, clickRemaining - delta);
        dragRemaining = System.Math.Max(0f, dragRemaining - delta);
    }
    public void Clear() { tokens.Clear(); clickRemaining = dragRemaining = 0f; }
}
