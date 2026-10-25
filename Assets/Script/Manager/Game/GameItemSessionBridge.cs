using UnityEngine;

/// <summary>아이템 씬 안의 통들에 시작 재료를 한 번 적용합니다. 코어나 다른 씬의 오브젝트를 검색하지 않습니다.</summary>
public sealed class GameItemSessionBridge : MonoBehaviour
{
    public MergegameManager[] boards;
    private GameSession session;
    private GameSession appliedSession;
    private int appliedRevision = -1;

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
        if (session != null)
            session.GameDataChanged -= ApplyMaterials;

        session = GameSession.Instance;
        if (session != null)
            session.GameDataChanged += ApplyMaterials;

        ApplyMaterials();
    }

    public void ApplyMaterials()
    {
        if (session == null)
            return;

        if (appliedSession == session && appliedRevision == session.GameDataRevision)
            return;

        if (!session.TryReadGeneralData(out var data))
            return;

        appliedSession = session;
        appliedRevision = session.GameDataRevision;
        if (boards == null)
            return;

        foreach (MergegameManager board in boards)
        {
            if (board != null)
            {
                Debug.Log("바인딩");
                board.SetSessionMaterials(data.materialItems);
                board.RefreshUI();
            }
        }
    }

    private void OnDisable()
    {
        GameSession.InstanceChanged -= BindSession;
        if (session != null)
            session.GameDataChanged -= ApplyMaterials;

        session = null;
    }
}
