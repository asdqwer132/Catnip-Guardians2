using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IDynamicBuffReceiver
{
    [Header("Stat")]
    public PlayerStat baseStat = new PlayerStat();
    public PlayerStat currentStat = new PlayerStat();

    [Header("Reference")]
    public BuffManager buffManager;

    [Header("Move")]
    public bool canMove = true;
    public float stopDistance = 0.03f;

    private Camera mainCamera;
    private Vector3 moveTargetPosition;
    private bool hasMoveTarget;

    private void Awake()
    {
        mainCamera = Camera.main;
        moveTargetPosition = transform.position;

        RefreshBuffedStat();
    }

    private void OnEnable()
    {
        RegisterToBuffManager();
    }

    private void OnDisable()
    {
        UnregisterFromBuffManager();
    }

    private void Update()
    {
        HandleMoveInput();
        MoveToTarget();
    }

    private void RegisterToBuffManager()
    {
        if (buffManager == null)
            return;

        buffManager.RegisterPlayer(this);
        buffManager.RegisterDynamicBuffReceiver(this);
    }

    private void UnregisterFromBuffManager()
    {
        if (buffManager == null)
            return;

        buffManager.UnregisterPlayer(this);
        buffManager.UnregisterDynamicBuffReceiver(this);
    }

    public void RefreshBuffedStat()
    {
        if (baseStat == null)
            return;

        if (buffManager == null)
        {
            currentStat = baseStat.Clone();
            currentStat.Clamp();
            return;
        }

        PlayerStat buffedStat = buffManager.GetBuffedPlayerStat(baseStat, this);

        currentStat = buffedStat != null ? buffedStat : baseStat.Clone();
        currentStat.Clamp();
    }

    public void OnDynamicBuffChanged()
    {
        RefreshBuffedStat();
    }

    private void HandleMoveInput()
    {
        if (!canMove)
            return;

        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        SetMoveTargetFromMouse();
    }

    private void SetMoveTargetFromMouse()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = transform.position.z;

        moveTargetPosition = mouseWorldPosition;
        hasMoveTarget = true;
    }

    private void MoveToTarget()
    {
        if (!canMove)
            return;

        if (!hasMoveTarget)
            return;

        float distance = Vector3.Distance(transform.position, moveTargetPosition);

        if (distance <= stopDistance)
        {
            hasMoveTarget = false;
            return;
        }

        float moveDistance = currentStat.moveSpeed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(
            transform.position,
            moveTargetPosition,
            moveDistance
        );
    }

    public void StopMove()
    {
        hasMoveTarget = false;
        moveTargetPosition = transform.position;
    }

    public bool IsInUsableRange(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        return distance >= currentStat.minRange && distance <= currentStat.maxRange;
    }
}