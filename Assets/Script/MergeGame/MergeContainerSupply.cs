using System;

// 투입 성공 시점에만 재료를 차감합니다. 가득 찬 통/수량 부족/등록되지 않은 재료는 보유량을 바꾸지 않습니다.
public static class MergeContainerSupply
{
    public static bool TryInject(MergeContainerBoard board, ItemData material, int maximum, bool hasSpace,
        MergeMaterialSupply mode, Func<ItemData, bool> hasMaterial, Func<ItemData, bool> consume,
        Action<ItemData> refund, out int id)
    {
        id = 0;
        if (board == null || board.Definition == null || !board.Definition.CanInject(material) || !hasSpace ||
            board.Count >= maximum || hasMaterial == null || !hasMaterial(material)) return false;
        bool spent = mode == MergeMaterialSupply.ConsumeStock;
        if (spent && (consume == null || !consume(material))) return false;
        id = board.Add(material, maximum);
        if (id == 0 && spent) refund?.Invoke(material);
        return id != 0;
    }
}
