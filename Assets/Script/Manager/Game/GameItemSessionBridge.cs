using UnityEngine;

/// <summary>아이템 씬 안의 통들에 시작 재료를 한 번 적용합니다. 코어나 다른 씬의 오브젝트를 검색하지 않습니다.</summary>
public sealed class GameItemSessionBridge : MonoBehaviour
{
    public MergegameManager[] boards;
    private GameSession session;
    private GameSession appliedSession;
    private int appliedRevision = -1;
    private int appliedRound = -1;

    private void OnEnable()
    {
        GameSession.InstanceChanged += BindSession;
        BindSession();
    }

    private void Start()
    {
        GameSession.GetOrCreate();
        BindSession();
    }

    private void BindSession()
    {
        UnbindSession();

        session = GameSession.Instance;
        if (session != null)
        {
            session.GameDataChanged += ApplyMaterials;
            session.MaterialsChanged += RefreshMaterials;
            session.RoundStarted += ApplyMaterials;
        }

        ApplyMaterials();
        RefreshMaterials();
    }

    public void ApplyMaterials()
    {
        if (session == null)
            return;

        bool newGame = appliedSession != session || appliedRevision != session.GameDataRevision;
        if (!newGame && appliedRound == session.RoundRevision)
            return;

        if (!session.TryReadGeneralData(out var data))
            return;

        appliedSession = session;
        appliedRevision = session.GameDataRevision;
        appliedRound = session.RoundRevision;
        if (boards == null)
            return;

        foreach (MergegameManager board in boards)
        {
            if (board != null)
            {
                if (newGame) board.SetSessionMaterials(data.materialItems, session);
                else
                {
                    board.ResetRoundMaterials();
                    board.RefreshSessionMaterials(data.materialItems);
                }
            }
        }
    }

    private void OnDisable()
    {
        GameSession.InstanceChanged -= BindSession;
        UnbindSession();
        session = null;
    }

    private void RefreshMaterials()
    {
        if (session == null || boards == null || !session.TryReadGeneralData(out var data)) return;
        foreach (var board in boards)
            if (board != null) board.RefreshSessionMaterials(data.materialItems);
    }

    private void UnbindSession()
    {
        if (session == null) return;
        session.GameDataChanged -= ApplyMaterials;
        session.MaterialsChanged -= RefreshMaterials;
        session.RoundStarted -= ApplyMaterials;
    }
}
