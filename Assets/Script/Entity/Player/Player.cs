using UnityEngine;

[DefaultExecutionOrder(20)]
[BuffTargetGroups("Player")]
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

    [Header("Battle Display")]
    [SerializeField] private PlayerBattleVisualData battleVisual;
    [SerializeField] private Animator displayAnimator;
    [SerializeField] private SpriteRenderer displaySprite;
    [SerializeField] private Transform movementCenter;
    [SerializeField] private bool allowManualMove = true;

    private readonly BattleAnimationPlayer animationPlayer = new BattleAnimationPlayer();
    private Sprite defaultSprite;
    private Vector3 initialPosition;
    private float nextMoveAt;
    private int lastThrowFrame = -1;
    private bool isThrowing;

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

        initialPosition = transform.position;
        moveTargetPosition = initialPosition;
        if (displaySprite != null) defaultSprite = displaySprite.sprite;

        RefreshBuffedStat();
    }

    private void OnEnable()
    {
        RegisterToBuffManager();
        SubscribeInput();
        ScheduleCircleMove();
    }

    private void OnDisable()
    {
        UnregisterFromBuffManager();
        UnsubscribeInput();
        StopMove();
        animationPlayer.Stop();
        isThrowing = false;
        if (displaySprite != null) displaySprite.sprite = defaultSprite;
    }

    private void Update()
    {
        if (isThrowing || (GameEndManager.instance != null && GameEndManager.instance.IsGameEnded) ||
            (GameInputManager.instance != null && GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay))) return;

        if (canMove && !hasMoveTarget && battleVisual != null && battleVisual.useCircleMovement &&
            battleVisual.radius > 0f && Time.time >= nextMoveAt)
        {
            Vector3 center = movementCenter != null ? movementCenter.position : initialPosition;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
            SetDestination(center + radial * battleVisual.radius);
        }
        MoveToTarget();
    }

    private void LateUpdate()
    {
        if (battleVisual == null) return;

        if (isThrowing)
        {
            animationPlayer.Tick(Time.deltaTime, false);
            if (!animationPlayer.IsFinished) return;
            isThrowing = false;
            animationPlayer.Stop();
        }

        bool moving = mover != null && mover.IsMoving;
        if (moving) Face(mover.CurrentMoveDirection.x);
        var clip = moving ? battleVisual.move : battleVisual.idle;
        animationPlayer.Play(displayAnimator, clip);
        if (!animationPlayer.IsPlaying && displaySprite != null) displaySprite.sprite = defaultSprite;
        animationPlayer.Tick(Time.deltaTime, true);
    }

    public void PlayThrowAnimation(Vector3 target)
    {
        if (!isActiveAndEnabled || battleVisual == null) return;
        // Multiple items thrown together share the pose and keep the first facing.
        if (lastThrowFrame == Time.frameCount) return;
        lastThrowFrame = Time.frameCount;
        Face(target.x - transform.position.x);
        if (battleVisual.attack == null || displayAnimator == null) return;
        mover?.Stop();
        animationPlayer.Play(displayAnimator, battleVisual.attack, restart: true);
        isThrowing = true;
    }

    private void Face(float horizontal)
    {
        if (displaySprite != null && Mathf.Abs(horizontal) > .001f)
            displaySprite.flipX = horizontal < 0f;
    }

    private void ScheduleCircleMove()
    {
        if (battleVisual == null) return;
        float variation = battleVisual.intervalVariation;
        nextMoveAt = Time.time + Mathf.Max(.01f, battleVisual.idleInterval + Random.Range(-variation, variation));
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
        if (!canMove || !allowManualMove)
            return;

        if (GameInputManager.instance == null)
            return;

        Vector3 mouseWorldPosition = GameInputManager.instance.MouseWorldPosition;
        mouseWorldPosition.z = transform.position.z;

        SetMoveTarget(mouseWorldPosition);
    }

    public void SetMoveTarget(Vector3 targetPosition)
    {
        SetDestination(targetPosition);
        if (movePingController != null)
            movePingController.ShowPing(moveTargetPosition);
    }

    private void SetDestination(Vector3 targetPosition)
    {
        targetPosition.z = transform.position.z;
        moveTargetPosition = targetPosition;
        hasMoveTarget = true;
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
            movePingController?.HidePing();
            ScheduleCircleMove();
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
