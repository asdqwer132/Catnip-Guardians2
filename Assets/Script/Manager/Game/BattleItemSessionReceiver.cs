using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>배틀 씬의 수신함. 데이터만 받으며 아이템 효과/투척은 실행하지 않습니다.</summary>
public sealed class BattleItemSessionReceiver : MonoBehaviour
{
    [SerializeField] private List<InventoryItem> receivedItems = new List<InventoryItem>();
    public IReadOnlyList<InventoryItem> ReceivedItems => receivedItems;
    public event Action<GameItemToBattleData> ItemsReceived;
    public event Action ItemsCleared;
    public int ContentRevision { get; private set; }
    [Tooltip("투척기가 아직 가져가지 않은 수신 묶음입니다. 비활성 상태였다가 켜져도 순서를 유지합니다.")]
    [SerializeField] private List<GameItemToBattleData> pendingThrowBatches = new List<GameItemToBattleData>();
    private GameSession session;
    private GameSession receivedSession;
    private int receivedRevision = -1;

    private void OnEnable()
    {
        GameSession.InstanceChanged += BindSession;
        BindSession();
    }

    private void BindSession()
    {
        UnbindSession();
        session = GameSession.Instance;
        if (session == null)
            return;

        if (receivedSession != session || receivedRevision != session.GameDataRevision)
            ResetForGame();

        session.BattleItemsAvailable += ReceivePendingItems;
        session.GameDataChanged += ResetForGame;
        ReceivePendingItems();
    }

    public void ReceivePendingItems()
    {
        while (session != null && session.TryTakeBattleItems(out var data))
        {
            foreach (InventoryItem item in data.items)
            {
                var existing = receivedItems.Find(entry => entry.itemData == item.itemData);
                if (existing == null)
                    receivedItems.Add(new InventoryItem(item.itemData, item.amount));
                else
                    existing.amount = (int)Math.Min(int.MaxValue, (long)existing.amount + item.amount);
            }

            pendingThrowBatches.Add(data.Copy());
            ItemsReceived?.Invoke(data);
        }
    }

    public bool TryTakeReceivedBatch(out GameItemToBattleData data)
    {
        data = pendingThrowBatches.Count > 0 ? pendingThrowBatches[0] : null;
        if (data == null)
            return false;

        pendingThrowBatches.RemoveAt(0);
        return true;
    }

    public void ClearReceivedItems()
    {
        receivedItems.Clear();
        pendingThrowBatches.Clear();
        ContentRevision++;
        ItemsCleared?.Invoke();
    }

    private void ResetForGame()
    {
        ClearReceivedItems();
        receivedSession = session;
        receivedRevision = session != null ? session.GameDataRevision : -1;
    }

    private void UnbindSession()
    {
        if (session == null)
            return;

        session.BattleItemsAvailable -= ReceivePendingItems;
        session.GameDataChanged -= ResetForGame;
    }

    private void OnDisable()
    {
        GameSession.InstanceChanged -= BindSession;
        UnbindSession();
        session = null;
    }
}
