using System.Collections.Generic;
using UnityEngine;

public partial class MergegameManager
{
    public bool TrySpawnGimmickBatch(IReadOnlyList<MergeSpawnRequest> requests)
    {
        if (!CanOperate || requests == null || ActiveItemCount + requests.Count > maximumActiveItems) return false;
        foreach (var request in requests) if (!TryGetPrefab(request.item, out _)) return false;
        var staged = new List<MergeItemObject>();
        int revisionBefore = Revision;
        bool committed = false;
        try
        {
            foreach (var request in requests)
            {
                if (!TrySpawnGimmickItem(request.item, request.position, out var piece)) return false;
                staged.Add(piece);
                if (Revision != revisionBefore || !CanOperate) return false;
            }
            committed = true;
            return true;
        }
        finally
        {
            if (!committed) foreach (var piece in staged) if (piece != null && piece.IsLive) piece.ReturnToPool();
        }
    }
}
