using System.Collections.Generic;
using UnityEngine;

// A pooled, initialized candidate is probed synchronously before any physics step.
// Inputs being replaced are ignored; already staged outputs reserve their own space.
internal sealed class MergeSpawnPlacement
{
    private readonly MergegameManager owner;
    private Collider2D[] hits = new Collider2D[32];
    public MergeSpawnPlacement(MergegameManager owner) => this.owner = owner;

    public bool TryPlace(MergeItemObject piece, Vector3 preferred, IReadOnlyList<MergeItemObject> inputs, bool search)
    {
        if (piece == null || piece.colider2d == null || !piece.colider2d.enabled || !piece.IsLive) return false;
        var shapes = piece.GetComponentsInChildren<Collider2D>();
        piece.SetSimulated(true);
        int attempts = search ? Mathf.Clamp(owner.spawnSearchAttempts, 1, 256) : 1;
        for (int i = 0; i < attempts; i++)
        {
            Vector3 position = preferred;
            if (i > 0)
            {
                float radius = Mathf.Max(0, owner.spawnSearchRadius) * Mathf.Sqrt((float)i / Mathf.Max(1, attempts - 1));
                float angle = i * 2.3999632f;
                position += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            }
            piece.transform.position = position;
            piece.body.position = position;
            Physics2D.SyncTransforms();
            if (InsideBox(piece, preferred) && IsClear(piece, shapes, inputs)) return true;
        }
        return false;
    }

    private bool InsideBox(MergeItemObject piece, Vector3 preferred)
    {
        var box = owner.dropZone != null ? owner.dropZone.GetComponentInParent<MergeBoxBindings>() : null;
        if (box == null) return true;
        foreach (var part in box.visuals)
        {
            if (part == null || part.role != MergeBoxSpriteRole.BackFrame || part.renderer == null || part.renderer.sprite == null) continue;
            Bounds frame = part.renderer.bounds, shape = piece.colider2d.bounds;
            // The open top may contain an authored inlet/spawn area above the frame.
            float top = Mathf.Max(frame.max.y, preferred.y + shape.extents.y);
            return shape.min.x >= frame.min.x && shape.max.x <= frame.max.x &&
                shape.min.y >= frame.min.y && shape.max.y <= top + .0001f;
        }
        return true;
    }

    private bool IsClear(MergeItemObject piece, Collider2D[] shapes, IReadOnlyList<MergeItemObject> inputs)
    {
        float clearance = Mathf.Max(0f, owner.spawnClearance);
        var physics = piece.gameObject.scene.GetPhysicsScene2D();
        foreach (var shape in shapes)
        {
            if (!shape.enabled || (shape.isTrigger && shape != piece.colider2d)) continue;
            Bounds bounds = shape.bounds;
            int layers = Physics2D.GetLayerCollisionMask(shape.gameObject.layer);
            layers = (layers | shape.includeLayers.value) & ~shape.excludeLayers.value;
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = layers };
            int count;
            do
            {
                count = physics.OverlapBox(bounds.center, (Vector2)bounds.size + Vector2.one * (clearance * 2f), 0f, filter, hits);
                if (count < hits.Length) break;
                System.Array.Resize(ref hits, hits.Length * 2);
            } while (true);
            for (int i = 0; i < count; i++)
            {
                var other = hits[i];
                if (other == null || other.transform.IsChildOf(piece.transform)) continue;
                var item = other.GetComponentInParent<MergeItemObject>();
                if (other.isTrigger && item == null) continue;
                bool ignored = false;
                if (inputs != null) foreach (var input in inputs) if (input == item) { ignored = true; break; }
                if (ignored) continue;
                var distance = shape.Distance(other);
                if (!distance.isValid || distance.isOverlapped || distance.distance < clearance) return false;
            }
        }
        return true;
    }
}
