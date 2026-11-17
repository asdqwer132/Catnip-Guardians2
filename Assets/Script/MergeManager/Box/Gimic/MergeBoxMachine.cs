using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class MergeBoxMachine : MergeBoxGimic
{
    [Header("Horizontal laser · drag emitter vertically")]
    public Transform laserEmitter;
    public Collider2D laserDragArea;
    [Min(.01f)] public float laserDistance = 8f;
    [Min(1)] public int splitMultiplier = 2;
    [Min(0f)] public float splitScatter = .25f;
    public LineRenderer laserVisual;
    [Min(0f)] public float laserVisualSeconds = .15f;
    [Header("Claw rail above the box")]
    public Transform clawVisual;
    public Collider2D clawRail;
    [Min(.01f)] public float clawSpeed = 5f;
    [Min(0f)] public float clawGripOffset = .3f;
    [Header("Existing input references")]
    public Camera inputCamera;
    public MergeItemSelection selection;
    private bool draggingLaser, previousSuppression;
    private float laserRemaining;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    protected override void EnsureSkills() => SetDefaultSkills(
        new GimicSkill { id = "Machine.Laser", type = GimicSkillType.Active, defaultSkillCool = 10f },
        new GimicSkill { id = "Machine.Claw", type = GimicSkillType.Active, defaultSkillCool = 10f });
    public override void GimicStart() { if (Board is MachineMergeGameManager machine) machine.LaserDragArea = laserDragArea; }
    protected override bool CanUseActiveSkill(int index) => Board is MachineMergeGameManager machine &&
        machine.ClawState == MergeClawState.Idle && (index == 0 ? laserEmitter != null : clawVisual != null && clawRail != null);
    protected override void OnActiveSkill(int index)
    {
        if (!(Board is MachineMergeGameManager machine)) return;
        if (index == 0)
        {
            machine.FireLaser(laserEmitter.position, laserDistance, splitMultiplier, splitScatter);
            laserRemaining = laserVisualSeconds;
            if (laserVisual != null)
            {
                laserVisual.gameObject.SetActive(true); laserVisual.useWorldSpace = true; laserVisual.positionCount = 2;
                laserVisual.SetPosition(0, laserEmitter.position); laserVisual.SetPosition(1, laserEmitter.position + Vector3.right * laserDistance);
            }
        }
        else if (machine.BeginClaw(clawVisual, clawRail, clawSpeed, clawGripOffset))
        { selection?.CancelSelection(); selection?.spawnPointDragger?.EndDrag(); }
    }
    protected override void TickGimic(float deltaTime)
    {
        if (!(Board is MachineMergeGameManager machine)) return;
        laserRemaining -= deltaTime;
        if (laserVisual != null && laserRemaining <= 0f) laserVisual.gameObject.SetActive(false);
        machine.TickClaw(deltaTime);
        var mouse = Mouse.current;
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) { EndLaserDrag(); machine.CancelClaw(); }
        if (mouse == null || inputCamera == null) return;
        if (!TryWorld(mouse.position.ReadValue(), out var world))
        { if (mouse.leftButton.wasReleasedThisFrame) { EndLaserDrag(); machine.RequestClawDrop(); } return; }
        if (mouse.leftButton.wasPressedThisFrame && !IsOverUI(mouse.position.ReadValue()))
        {
            if (machine.ClawState == MergeClawState.AwaitingItem) machine.TryGrab(world);
            else if (machine.ClawState == MergeClawState.Idle && laserDragArea != null && laserDragArea.OverlapPoint(world))
            {
                draggingLaser = true; previousSuppression = Board.SelectionSuppressed; Board.SelectionSuppressed = true;
                selection?.CancelSelection(); selection?.spawnPointDragger?.EndDrag();
            }
        }
        if (draggingLaser && laserEmitter != null && laserDragArea != null)
        {
            Vector2 clamped = laserDragArea.ClosestPoint(new Vector2(laserEmitter.position.x, world.y));
            laserEmitter.position = new Vector3(laserEmitter.position.x, clamped.y, laserEmitter.position.z);
        }
        if (machine.ClawState != MergeClawState.Idle && machine.ClawState != MergeClawState.AwaitingItem) machine.SetClawDropX(world.x);
        if (mouse.leftButton.wasReleasedThisFrame) { EndLaserDrag(); machine.RequestClawDrop(); }
    }
    private bool TryWorld(Vector2 screen, out Vector2 world)
    {
        world = default;
        if (!Application.isFocused || !SceneViewportInput.ScreenRect(inputCamera).Contains(screen)) return false;
        var plane = new Plane(Vector3.forward, Board.transform.position);
        var ray = SceneViewportInput.ScreenPointToRay(inputCamera,screen);
        if (!plane.Raycast(ray, out float distance)) return false;
        world = ray.GetPoint(distance); return true;
    }
    private bool IsOverUI(Vector2 screen)
    {
        if (EventSystem.current == null) return false;
        uiHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, uiHits);
        foreach (var hit in uiHits) if (hit.module is GraphicRaycaster) return true;
        return false;
    }
    private void EndLaserDrag()
    { if (draggingLaser && Board != null) Board.SelectionSuppressed = previousSuppression; draggingLaser = false; }
    public override void GimicStop()
    {
        EndLaserDrag();
        if (Board is MachineMergeGameManager machine) { machine.CancelClaw(); machine.LaserDragArea = null; }
        if (laserVisual != null) laserVisual.gameObject.SetActive(false);
        base.GimicStop();
    }
}
