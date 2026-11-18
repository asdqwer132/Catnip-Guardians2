using System.Collections.Generic;
using UnityEngine;

public partial class MergegameManager
{
    public bool TrySpawnGimmickItem(MergeItem definition, Vector3 position, out MergeItemObject piece)
    {
        piece = null;
        if (!CanOperate || ActiveItemCount >= maximumActiveItems) return false;
        int before = Revision;
        piece = SpawnPiece(definition, bonusSpawn.Position(position));
        if (piece == null) return false;
        if (!piece.IsLive) { piece.ReturnToPool(); piece = null; return false; }
        if (Revision != before || !CanOperate) { piece.ReturnToPool(); piece = null; return false; }
        bonusSpawn.Launch(piece, position);
        CodexDiscoveryHistory.Current.RecordItem(definition.item);
        return true;
    }
    // Replacement is transactional: a missing prefab/capacity must never destroy the input item.
    public bool TryReplaceGimmickItem(MergeItemObject input, int lifeId, IReadOnlyList<MergeSpawnRequest> outputs, object holder = null)
    {
        if (!CanOperate || input == null || input.Owner != this || input.LifeId != lifeId || !input.IsLive ||
            outputs == null || outputs.Count == 0 || ActiveItemCount - 1 + outputs.Count > maximumActiveItems) return false;
        foreach (var output in outputs) if (!TryGetPrefab(output.item, out _)) return false;
        if (!input.ReserveReplacement(holder)) return false;
        int before = Revision;
        var spawned = new List<MergeItemObject>(); bool committed = false;
        try
        {
            foreach (var output in outputs)
            {
                var piece = SpawnPiece(output.item, bonusSpawn.Position(output.position));
                if (piece == null) return false;
                spawned.Add(piece);
                if (Revision != before || !CanOperate || input.LifeId != lifeId || !input.IsLive) return false;
            }
            foreach (var piece in spawned) if (piece == null || !piece.IsLive) return false;
            Vector3 center = input.transform.position;
            input.ReturnToPool();
            foreach (var piece in spawned)
            {
                bonusSpawn.Launch(piece, center);
                CodexDiscoveryHistory.Current.RecordItem(piece.item);
                discoveryHistory?.RecordCrafted(piece.item);
            }
            committed = true; return true;
        }
        finally
        {
            if (!committed) foreach (var piece in spawned) if (piece != null && piece.IsLive) piece.ReturnToPool();
            if (input != null && input.LifeId == lifeId) input.Unreserve();
        }
    }
}
