using System.Collections.Generic;
using UnityEngine;

public enum MergeClawState { Idle, AwaitingItem, Lifting, MovingAlongRail }

[DefaultExecutionOrder(-200)]
public sealed class MachineMergeGameManager : MergegameManager
{
    public Collider2D LaserDragArea { get; set; }
    public override bool CapturesSelectionPointer(Vector2 worldPosition) => ClawState != MergeClawState.Idle ||
        (LaserDragArea != null && LaserDragArea.isActiveAndEnabled && LaserDragArea.OverlapPoint(worldPosition));
    public MergeClawState ClawState { get; private set; }
    private Transform claw;
    private Collider2D rail;
    private MergeItemObject grabbed;
    private int grabbedLife;
    private float desiredX, clawSpeed, gripOffset;
    private bool dropRequested, previousSuppression;
    private readonly RaycastHit2D[] laserHits = new RaycastHit2D[256];
    private readonly List<MergeSpawnRequest> splitOutputs = new List<MergeSpawnRequest>();
    public int FireLaser(Vector2 origin, float distance, int multiplier = 2, float spread = .25f)
    {
        if (!CanOperate || recipeBook == null || distance <= 0f) return 0;
        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = false };
        int count = gameObject.scene.GetPhysicsScene2D().Raycast(origin, Vector2.right, distance, filter, laserHits);
        var candidates = new Dictionary<MergeItemObject, int>();
        for (int i = 0; i < count; i++)
        {
            var piece = laserHits[i].collider.GetComponentInParent<MergeItemObject>();
            if (piece != null && piece.Owner == this && piece.CanMerge) candidates[piece] = piece.LifeId;
        }
        int split = 0;
        foreach (var candidate in candidates)
            if (candidate.Key != null && candidate.Key.LifeId == candidate.Value && TrySplit(candidate.Key, multiplier, spread)) split++;
        return split;
    }
    public bool TrySplit(MergeItemObject piece, int multiplier = 2, float spread = .25f)
    {
        if (piece == null || piece.Owner != this || recipeBook == null || !piece.CanMerge) return false;
        ItemRecipeData recipe = null;
        foreach (var entry in recipeBook.Recipes)
            if (entry != null && entry.resultItem == piece.item) { recipe = entry; break; }
        var materials = new List<InventoryItem>();
        if (recipe == null || !recipe.TryGetRequiredMaterials(materials)) return false;
        splitOutputs.Clear();
        long total = 0;
        foreach (var material in materials) total += (long)material.amount * Mathf.Max(1, multiplier);
        if (total > maximumActiveItems) return false;
        foreach (var material in materials)
        {
            var definition = GetDefinitionAtMergeItem(material.itemData);
            for (int i = 0; i < material.amount * Mathf.Max(1, multiplier); i++)
            {
                Vector3 position = piece.transform.position + (Vector3)Random.insideUnitCircle * Mathf.Max(0f, spread);
                splitOutputs.Add(new MergeSpawnRequest(definition, position));
            }
        }
        int life = piece.LifeId;
        return TryReplaceGimmickItem(piece, life, splitOutputs) ||
            (piece.IsLive && piece.LifeId == life && QueueReplacement(piece, splitOutputs));
    }
    public bool BeginClaw(Transform clawVisual, Collider2D movementRail, float speed, float itemOffset)
    {
        if (!CanOperate || ClawState != MergeClawState.Idle || clawVisual == null || movementRail == null) return false;
        claw = clawVisual; rail = movementRail; clawSpeed = Mathf.Max(.01f, speed); gripOffset = Mathf.Max(0f, itemOffset);
        previousSuppression = SelectionSuppressed; SelectionSuppressed = true;
        dropRequested = false; ClawState = MergeClawState.AwaitingItem; return true;
    }
    public bool TryGrab(Vector2 world)
    {
        if (!CanOperate || ClawState != MergeClawState.AwaitingItem) return false;
        MergeItemObject closest = null; float nearest = float.PositiveInfinity;
        foreach (var piece in ActiveItems)
        {
            if (piece == null || !piece.CanSelect || piece.IsHeld || piece.colider2d == null || !piece.colider2d.OverlapPoint(world)) continue;
            float distance = (piece.Position - world).sqrMagnitude;
            if (distance < nearest) { closest = piece; nearest = distance; }
        }
        if (closest == null || !closest.TryHold(this, false)) return false;
        grabbed = closest; grabbedLife = closest.LifeId;
        claw.position = new Vector3(closest.transform.position.x, closest.transform.position.y + gripOffset, claw.position.z);
        desiredX = Mathf.Clamp(world.x, rail.bounds.min.x, rail.bounds.max.x);
        ClawState = MergeClawState.Lifting; return true;
    }
    public void SetClawDropX(float x)
    { if (rail != null) desiredX = Mathf.Clamp(x, rail.bounds.min.x, rail.bounds.max.x); }
    public void RequestClawDrop() => dropRequested = true;
    public void TickClaw(float deltaTime)
    {
        if (!CanOperate || deltaTime <= 0f || ClawState == MergeClawState.Idle || ClawState == MergeClawState.AwaitingItem) return;
        if (grabbed == null || !grabbed.IsLive || grabbed.LifeId != grabbedLife || claw == null || rail == null)
        { CancelClaw(); return; }
        float top = rail.bounds.center.y;
        Vector3 destination = new Vector3(ClawState == MergeClawState.Lifting ? claw.position.x : desiredX, top, claw.position.z);
        claw.position = Vector3.MoveTowards(claw.position, destination, clawSpeed * deltaTime);
        grabbed.transform.position = new Vector3(claw.position.x, claw.position.y - gripOffset, grabbed.transform.position.z);
        grabbed.body.position = grabbed.transform.position;
        if ((claw.position - destination).sqrMagnitude > .0001f) return;
        if (ClawState == MergeClawState.Lifting) ClawState = MergeClawState.MovingAlongRail;
        else if (dropRequested) CancelClaw(); // Release restores physics; it falls vertically from the rail.
    }
    public void CancelClaw()
    {
        if (grabbed != null && grabbed.LifeId == grabbedLife) grabbed.ReleaseHold(this);
        grabbed = null;
        if (ClawState != MergeClawState.Idle) SelectionSuppressed = previousSuppression;
        ClawState = MergeClawState.Idle; dropRequested = false;
    }
    protected override void OnDisable() { CancelClaw(); base.OnDisable(); }
}
