using System.Collections.Generic;
using UnityEngine;

public enum MergeClawState { Idle, AwaitingItem, Lifting, MovingAlongRail, ApproachingItem, Lowering, Retracting }

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
    private Vector3 pickupPosition;
    public bool ClawHasItem => grabbed != null && (ClawState == MergeClawState.Lifting || ClawState == MergeClawState.MovingAlongRail);
    private readonly List<Collider2D> laserHits = new List<Collider2D>();
    private readonly List<MergeSpawnRequest> splitOutputs = new List<MergeSpawnRequest>();
    public int FireLaser(Collider2D area,int multiplier=2,float spread=.25f)
    {
        if(!CanOperate || recipeBook==null || area==null || !area.isActiveAndEnabled) return 0;
        Physics2D.SyncTransforms();laserHits.Clear();
        area.Overlap(new ContactFilter2D { useTriggers=true,useLayerMask=false },laserHits);
        var candidates=new Dictionary<MergeItemObject,int>();
        foreach(var hit in laserHits)
        {
            var piece=hit!=null?hit.GetComponentInParent<MergeItemObject>():null;
            if(piece!=null && piece.Owner==this && piece.CanMerge) candidates[piece]=piece.LifeId;
        }
        int count=0;
        // Snapshot first: fragments created by this shot must not split again in the same shot.
        foreach(var target in candidates)
            if(target.Key!=null && target.Key.LifeId==target.Value && TrySplit(target.Key,multiplier,spread)) count++;
        return count;
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
        pickupPosition = closest.transform.position;
        desiredX = Mathf.Clamp(world.x, rail.bounds.min.x, rail.bounds.max.x);
        ClawState = MergeClawState.Retracting; return true;
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
        Vector3 destination = ClawState == MergeClawState.Retracting
            ? new Vector3(claw.position.x, top, claw.position.z)
            : ClawState == MergeClawState.ApproachingItem
            ? new Vector3(pickupPosition.x, top, claw.position.z)
            : ClawState == MergeClawState.Lowering
            ? new Vector3(pickupPosition.x, pickupPosition.y + gripOffset, claw.position.z)
            : new Vector3(ClawState == MergeClawState.Lifting ? claw.position.x : desiredX, top, claw.position.z);
        claw.position = Vector3.MoveTowards(claw.position, destination, clawSpeed * deltaTime);
        if (ClawHasItem)
        {
            grabbed.transform.position = new Vector3(claw.position.x, claw.position.y - gripOffset, grabbed.transform.position.z);
            grabbed.body.position = grabbed.transform.position;
        }
        if ((claw.position - destination).sqrMagnitude > .0001f) return;
        if (ClawState == MergeClawState.Retracting) ClawState = MergeClawState.ApproachingItem;
        else if (ClawState == MergeClawState.ApproachingItem) ClawState = MergeClawState.Lowering;
        else if (ClawState == MergeClawState.Lowering) ClawState = MergeClawState.Lifting;
        else if (ClawState == MergeClawState.Lifting) ClawState = MergeClawState.MovingAlongRail;
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
