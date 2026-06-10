using UnityEngine;

public class Player : MonoBehaviour, IDynamicBuffReceiver, IBuffTarget
{
    [Header("Stat")]
    public PlayerStat baseStat = new PlayerStat();
    public PlayerStat currentStat = new PlayerStat();

    [Header("Reference")]
    public BuffManager buffManager;
    public ActorMover mover;
    public MovePingController movePingController;
    public PlayerRangeIndicatorController rangeIndicatorController;

    [Header("Move")]
    public bool canMove = true;
    public float stopDistance = 0.03f;

    private Vector3 moveTargetPosition;
    private bool hasMoveTarget;

    public Vector3 CurrentPosition => transform.position;
    public float MinRange => currentStat != null ? currentStat.minRange : 0f;
    public float MaxRange => currentStat != null ? currentStat.maxRange : 0f;
    public Vector3 MoveTargetPosition => moveTargetPosition;
    public bool HasMoveTarget => hasMoveTarget;

    public UnityEngine.Object BuffTargetObject => this;
    public string BuffTargetGroup => "Player";
    public string BuffTargetDebugName => name;

    private void Awake()
    {
        if (mover == null)
            mover = GetComponent<ActorMover>();

        if (movePingController == null)
            movePingController = GetComponent<MovePingController>();

        if (rangeIndicatorController == null)
            rangeIndicatorController = GetComponent<PlayerRangeIndicatorController>();

        moveTargetPosition = transform.position;

        RefreshBuffedStat();
    }

    private void OnEnable()
    {
        RegisterToBuffManager();
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnregisterFromBuffManager();
        UnsubscribeInput();
    }

    private void Update()
    {
        MoveToTarget();
    }

    private void RegisterToBuffManager()
    {
        if (buffManager == null)
            return;

        buffManager.RegisterBuffTarget(this);
        buffManager.RegisterDynamicBuffReceiver(this);
    }

    private void UnregisterFromBuffManager()
    {
        if (buffManager == null)
            return;

        buffManager.UnregisterBuffTarget(this);
        buffManager.UnregisterDynamicBuffReceiver(this);
    }

    private void SubscribeInput()
    {
        if (GameInputManager.instance == null)
            return;

        GameInputManager.instance.OnMovePressed += SetMoveTargetFromInput;
        GameInputManager.instance.OnPlayerRangePressed += ToggleIndicator;
    }

    private void UnsubscribeInput()
    {
        if (GameInputManager.instance == null)
            return;

        GameInputManager.instance.OnMovePressed -= SetMoveTargetFromInput;
        GameInputManager.instance.OnPlayerRangePressed -= ToggleIndicator;
    }

    public void RefreshBuffedStat()
    {
        if (baseStat == null)
            return;

        if (buffManager == null)
            currentStat = baseStat.Clone();
        else
        {
            PlayerStat buffedStat = buffManager.GetBuffedStatForTarget(baseStat, this);
            currentStat = buffedStat != null ? buffedStat : baseStat.Clone();
        }

        currentStat.Clamp();

        ApplyStatToMover();
        RefreshRangeIndicator();
    }

    private void ApplyStatToMover()
    {
        if (mover == null || currentStat == null)
            return;

        mover.SetSpeed(currentStat.moveSpeed);
    }

    private void RefreshRangeIndicator()
    {
        if (rangeIndicatorController != null)
            rangeIndicatorController.RefreshRange(currentStat.maxRange, currentStat.minRange);
    }
    private void ToggleIndicator()
    {
        if (rangeIndicatorController != null)
        {
            rangeIndicatorController.Toggle(currentStat.maxRange, currentStat.minRange);
            RefreshRangeIndicator();
        }
    }
    public void OnDynamicBuffChanged()
    {
        RefreshBuffedStat();
    }

    private void SetMoveTargetFromInput()
    {
        if (!canMove)
            return;

        if (GameInputManager.instance == null)
            return;

        Vector3 mouseWorldPosition = GameInputManager.instance.MouseWorldPosition;
        mouseWorldPosition.z = transform.position.z;

        SetMoveTarget(mouseWorldPosition);
    }

    public void SetMoveTarget(Vector3 targetPosition)
    {
        targetPosition.z = transform.position.z;

        moveTargetPosition = targetPosition;
        hasMoveTarget = true;

        if (movePingController != null)
            movePingController.ShowPing(moveTargetPosition);
    }

    private void MoveToTarget()
    {
        if (!canMove)
        {
            StopMove();
            return;
        }

        if (!hasMoveTarget)
            return;

        if (mover == null)
            return;

        float distance = Vector3.Distance(transform.position, moveTargetPosition);

        if (distance <= stopDistance)
        {
            StopMove();
            movePingController.HidePing();
            return;
        }

        mover.MoveToPosition(moveTargetPosition, stopDistance);
    }

    public void StopMove()
    {
        hasMoveTarget = false;
        moveTargetPosition = transform.position;

        if (mover != null)
            mover.Stop();
    }

    public bool IsInUsableRange(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        return distance >= MinRange && distance <= MaxRange;
    }
}
