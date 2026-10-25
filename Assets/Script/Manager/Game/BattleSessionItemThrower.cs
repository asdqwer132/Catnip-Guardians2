using System;
using System.Collections.Generic;
using UnityEngine;

public enum SessionItemThrowMode { Simultaneous, Sequential }

/// <summary>수신한 묶음을 순서대로 기존 투척 실행기에 넘깁니다. 성공한 투척만 수량을 소비합니다.</summary>
public sealed class BattleSessionItemThrower : MonoBehaviour
{
    [Header("같은 배틀 씬 연결")]
    public BattleItemSessionReceiver receiver;
    public ItemUsePositionProvider positionProvider;
    public ItemThrowExecutor throwExecutor;
    public GameObject owner;
    public GameEndManager gameEndManager;

    [Header("투척 순서")]
    public SessionItemThrowMode mode;
    [Min(0f)] public float sequentialInterval = 0.1f;
    public Vector3 startOffset;
    public Vector3 targetOffset;
    [Tooltip("켜면 착지 지점을 플레이어의 현재 사거리로 보정합니다. 끄면 선택한 월드 위치를 유지합니다.")]
    public bool clampScatteredTargetsToPlayerRange;

    [Header("착지 지점 분산")]
    public SessionItemThrowSpread spread = new SessionItemThrowSpread();
    [Header("기존 투척 프리팹/모션")]
    public ItemThrowOptions throwOptions = new ItemThrowOptions();

    public event Action<ItemData, Vector3> ItemThrown;
    public int PendingItemCount
    {
        get
        {
            long count = 0;
            foreach (PendingBatch batch in pending)
                count += batch.remaining;
            return (int)Math.Min(int.MaxValue, count);
        }
    }

    [Serializable]
    private sealed class PendingBatch
    {
        public GameItemToBattleData data;
        public int index, thrown, total, remaining;
        public bool hasTarget;
        public Vector3 center, forward;
        public float radius;
    }

    [Header("투척 대기 상태 · 런타임")]
    [SerializeField] private List<PendingBatch> pending = new List<PendingBatch>();
    private BattleItemSessionReceiver boundReceiver;
    private BattleItemSessionReceiver previousReceiver;
    private int receivedRevision;
    private GameEndManager boundEndManager;
    private float nextThrowAt;
    private int revision;

    private void OnEnable() => Init();
    private void Start() => Init();
    private void Update() => Tick(Time.time);

    public void Init()
    {
        Unbind();
        boundReceiver = receiver;
        if (boundReceiver != null)
        {
            if (previousReceiver != null && (previousReceiver != boundReceiver || receivedRevision != boundReceiver.ContentRevision))
                ClearPending();
            previousReceiver = boundReceiver;
            receivedRevision = boundReceiver.ContentRevision;
            boundReceiver.ItemsReceived += OnItemsReceived;
            boundReceiver.ItemsCleared += ResetForGame;
            ReadReceivedBatches();
        }
        BindEndManager();
    }

    private void BindEndManager()
    {
        var next = gameEndManager != null ? gameEndManager : GameEndManager.instance;
        if (boundEndManager == next)
            return;

        if (boundEndManager != null)
            boundEndManager.OnGameEnded -= OnGameEnded;
        boundEndManager = next;
        if (boundEndManager != null)
        {
            boundEndManager.OnGameEnded += OnGameEnded;
            if (boundEndManager.IsGameEnded)
                OnGameEnded(boundEndManager.CurrentEndType);
        }
    }

    private void OnItemsReceived(GameItemToBattleData data) => ReadReceivedBatches();

    private void ReadReceivedBatches()
    {
        while (boundReceiver != null && boundReceiver.TryTakeReceivedBatch(out var data))
        {
            long count = 0;
            foreach (InventoryItem item in data.items)
                count += item.amount;
            int total = (int)Math.Min(int.MaxValue, count);
            if (total > 0)
                pending.Add(new PendingBatch { data = data, total = total, remaining = total });
        }
    }

    public void Tick(float now)
    {
        if (!isActiveAndEnabled)
            return;

        BindEndManager();
        if (Time.timeScale <= 0f || positionProvider == null || throwExecutor == null ||
            (boundEndManager != null && boundEndManager.IsGameEnded) ||
            (GameInputManager.instance != null && GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay)))
            return;

        if (mode == SessionItemThrowMode.Sequential && now < nextThrowAt)
            return;

        while (pending.Count > 0)
        {
            PendingBatch batch = pending[0];
            GameObject throwOwner = owner != null ? owner :
                positionProvider.useStartPoint != null ? positionProvider.useStartPoint.gameObject : gameObject;
            Vector3 start = positionProvider.GetUseStartPosition(throwOwner) + startOffset;
            start.z = 0f;
            if (!batch.hasTarget)
            {
                if (!positionProvider.TryGetTargetPosition(out batch.center))
                    return;
                batch.center += targetOffset;
                batch.center.z = 0f;
                batch.forward = batch.center - start;
                batch.radius = spread != null ? spread.ResolveRadius(batch.total) : 0f;
                batch.hasTarget = true;
            }

            InventoryItem item = batch.data.items[batch.index];
            Vector3 target = spread != null
                ? spread.GetPosition(batch.center, batch.forward, batch.thrown, batch.total, batch.radius) : batch.center;
            if (clampScatteredTargetsToPlayerRange)
                target = positionProvider.ClampPositionByPlayerRange(target);

            int before = revision;
            if (!throwExecutor.Throw(item.itemData, start, target, throwOwner, null, 0, options: throwOptions))
                return;
            if (before != revision)
                return;

            item.amount--;
            batch.remaining--;
            batch.thrown++;
            if (item.amount == 0)
                batch.index++;
            if (batch.remaining == 0)
                pending.RemoveAt(0);

            ItemThrown?.Invoke(item.itemData, target);
            if (mode == SessionItemThrowMode.Sequential)
            {
                nextThrowAt = now + EffectStatUtility.Safe(sequentialInterval, 0f, 600f, 0.1f);
                return;
            }
        }
    }

    public void ClearPending()
    {
        pending.Clear();
        nextThrowAt = 0f;
        revision++;
    }

    private void ResetForGame()
    {
        ClearPending();
        receivedRevision = boundReceiver != null ? boundReceiver.ContentRevision : 0;
        if (positionProvider != null)
            positionProvider.ClearSelectedPosition();
    }

    private void OnGameEnded(GameEndType type)
    {
        if (receiver != null)
            receiver.ClearReceivedItems();
        else
            ResetForGame();
    }

    private void Unbind()
    {
        if (boundReceiver != null)
        {
            boundReceiver.ItemsReceived -= OnItemsReceived;
            boundReceiver.ItemsCleared -= ResetForGame;
        }
        if (boundEndManager != null)
            boundEndManager.OnGameEnded -= OnGameEnded;
        boundEndManager = null;
    }

    private void OnDisable() => Unbind();
}
