using System.Collections.Generic;
using UnityEngine;

public partial class MergegameManager
{
    private readonly Queue<MergeSpawnRequest> deferredSpawns = new Queue<MergeSpawnRequest>();
    private sealed class DeferredReplacement
    {
        public MergeItemObject source; public int life;
        public Vector3 origin;
        public MergeSpawnRequest[] outputs;
    }
    private readonly List<DeferredReplacement> deferredReplacements = new List<DeferredReplacement>();
    private struct Retry { public int life; public float time; }
    private readonly Dictionary<MergeItemObject, Retry> spawnRetries = new Dictionary<MergeItemObject, Retry>();
    private float nextDeferredAttempt;
    internal bool SpawnRetryReady(MergeItemObject source)
    {
        if (spawnRetries.TryGetValue(source, out var retry) && retry.life == source.LifeId && Time.time < retry.time) return false;
        spawnRetries.Remove(source); return true;
    }
    internal void DelaySpawnRetry(MergeItemObject source)
        => spawnRetries[source] = new Retry { life = source.LifeId, time = Time.time + Mathf.Max(.01f, spawnRetryInterval) };
    public int PendingSpawnCount => deferredSpawns.Count;

    // Rewards are rolled once by the caller; waiting never rerolls or duplicates them.
    public bool QueueGimmickSpawn(MergeItem definition, Vector3 position)
    {
        if (!CanOperate || !TryGetPrefab(definition, out _)) return false;
        int before = Revision;
        if (!TrySpawnGimmickItem(definition, position, out _) && before == Revision && CanOperate)
            deferredSpawns.Enqueue(new MergeSpawnRequest(definition, position));
        return before == Revision && CanOperate;
    }

    protected bool QueueReplacement(MergeItemObject source, IReadOnlyList<MergeSpawnRequest> outputs)
    {
        if (!CanOperate || source == null || !source.IsLive || source.Owner != this) return false;
        foreach (var request in outputs) if (!TryGetPrefab(request.item, out _)) return false;
        foreach (var pending in deferredReplacements)
            if (pending.source == source && pending.life == source.LifeId) return true;
        var copy = new MergeSpawnRequest[outputs.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = outputs[i];
        deferredReplacements.Add(new DeferredReplacement { source = source, life = source.LifeId, origin = source.transform.position, outputs = copy });
        return true;
    }

    public void ProcessDeferredSpawns()
    {
        if (!CanOperate) return;
        if (Time.time < nextDeferredAttempt) return;
        nextDeferredAttempt = Time.time + Mathf.Max(.01f, spawnRetryInterval);
        int before = Revision;
        int attempts = Mathf.Min(4, deferredSpawns.Count);
        for (int i = 0; i < attempts && deferredSpawns.Count > 0; i++)
        {
            var request = deferredSpawns.Dequeue();
            bool spawned = TrySpawnGimmickItem(request.item, request.position, out _);
            if (Revision != before || !CanOperate) return;
            if (!spawned) deferredSpawns.Enqueue(request);
        }
        foreach (var pending in deferredReplacements.ToArray())
        {
            var source = pending.source;
            if (source == null || !source.IsLive || source.LifeId != pending.life || source.Owner != this)
            { deferredReplacements.Remove(pending); continue; }
            var moved = new MergeSpawnRequest[pending.outputs.Length];
            Vector3 delta = source.transform.position - pending.origin;
            for (int i = 0; i < moved.Length; i++) moved[i] = new MergeSpawnRequest(pending.outputs[i].item, pending.outputs[i].position + delta);
            if (TryReplaceGimmickItem(source, pending.life, moved)) deferredReplacements.Remove(pending);
            if (Revision != before || !CanOperate) return;
        }
    }

    private void RemapDeferredSpawns(Matrix4x4 change)
    {
        int count = deferredSpawns.Count;
        for (int i = 0; i < count; i++)
        {
            var request = deferredSpawns.Dequeue();
            request.position = change.MultiplyPoint3x4(request.position); deferredSpawns.Enqueue(request);
        }
        foreach (var pending in deferredReplacements)
        {
            pending.origin = change.MultiplyPoint3x4(pending.origin);
            for (int i = 0; i < pending.outputs.Length; i++)
                pending.outputs[i].position = change.MultiplyPoint3x4(pending.outputs[i].position);
        }
        spawnRetries.Clear(); nextDeferredAttempt = 0f;
    }

    private void ClearDeferredSpawns() { deferredSpawns.Clear(); deferredReplacements.Clear(); spawnRetries.Clear(); nextDeferredAttempt = 0f; }

    public bool TrySpawnGimmickBatch(IReadOnlyList<MergeSpawnRequest> requests)
    {
        if (!CanOperate || requests == null || ActiveItemCount + requests.Count > maximumActiveItems) return false;
        foreach (var request in requests) if (!TryGetPrefab(request.item, out _)) return false;
        int before = Revision;
        var staged = new List<MergeItemObject>(); bool committed = false;
        try
        {
            foreach (var request in requests)
            {
                var piece = SpawnPiece(request.item, request.position);
                if (piece != null) staged.Add(piece);
                if (piece == null || !piece.IsLive || before != Revision || !CanOperate || !TryPlaceSpawn(piece, request.position)) return false;
            }
            committed = true;
            foreach (var piece in staged) CodexDiscoveryHistory.Current.RecordItem(piece.item);
            return true;
        }
        finally { if (!committed) foreach (var piece in staged) if (piece != null) piece.ReturnToPool(); }
    }
}
