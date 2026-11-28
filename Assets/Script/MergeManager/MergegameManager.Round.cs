using UnityEngine;
using UnityEngine.SceneManagement;

public partial class MergegameManager
{
    [Header("Pinball round")]
    [SerializeField] private PinballBoard pinball;
    [SerializeField] private MergeGimicManager gimmickManager;
    [SerializeField] private PinballInlet[] inlets = new PinballInlet[5];
    [Tooltip("Use the inlet preview materials even when a saved loadout exists.")]
    [SerializeField] private bool debugMode;
    [SerializeField, Min(0f)] private float dropInterval = .5f;
    [SerializeField, Min(0f)] private float minimumDropInterval = .1f;

    private float[] inletIntervalOffsets;
    private GameSession session;
    private bool started;
    private int dataRevision = -1, roundRevision = -1;
    internal PinballBoard Pinball => pinball;

    private void OnEnable()
    {
        GameSession.InstanceChanged += BindSession;
        BindSession();
    }

    private void Start()
    {
        started = true;
        GameSession.GetOrCreate();
        ApplySessionRound();
    }

    private void BindSession()
    {
        UnbindSession();
        session = GameSession.Instance;
        dataRevision = roundRevision = -1;
        if (session == null) return;
        session.GameDataChanged += ApplySessionRound;
        session.RoundStarted += ApplySessionRound;
        ApplySessionRound();
    }

    private void UnbindSession()
    {
        if (session == null) return;
        session.GameDataChanged -= ApplySessionRound;
        session.RoundStarted -= ApplySessionRound;
        session = null;
    }

    private void ApplySessionRound()
    {
        if (!started || session == null || (dataRevision == session.GameDataRevision && roundRevision == session.RoundRevision)) return;
        dataRevision = session.GameDataRevision;
        roundRevision = session.RoundRevision;
        session.TryReadItemRound(out var data);
        InitializeRound(data);
    }

    public void InitializeRound(UpgradeToGeneralData data)
    {
        inletIntervalOffsets = new float[inlets.Length];
        if (data?.unlockedSkillNodes != null)
            foreach (var node in data.unlockedSkillNodes)
            {
                if (node?.rewards == null) continue;
                foreach (var reward in node.rewards)
                    if (reward is PinballInletIntervalReward intervalReward)
                        intervalReward.AddOffsets(inletIntervalOffsets);
            }

        gimmickManager?.StopGimic();
        ClearItems();
        bool preview = debugMode || (data?.equipment == null && !SceneManager.GetSceneByName(GameSceneRoutes.General).isLoaded);
        for (int i = 0; i < inlets.Length; i++)
        {
            var inlet = inlets[i];
            if (inlet == null) continue;
            var slot = data?.boardMaterialSlots;
            var stock = slot != null && i < slot.Count ? slot[i] : null;
            var material = preview ? inlet.previewMaterial : GetDefinitionAtMergeItem(stock?.itemData);
            int count = preview ? inlet.previewCount : stock?.amount ?? 0;
            if (!TryGetPrefab(material, out var prefab)) material = null;
            else if (count > 0 && objectPool != null && prewarmCount > 0) objectPool.Prewarm(prefab, prewarmCount);
            inlet.Bind(material, count);
        }

        if (pinball != null && pinball.IsConfigured) pinball.Bind(this, preview ? null : data?.boardPinSlots);
        gimmickManager?.ApplyLoadout(this, data);
    }

    private void Update()
    {
        if (inletIntervalOffsets == null || !CanOperate || pinball == null || !pinball.LayoutInlets(inlets) ||
            (GameInputManager.instance != null && GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay))) return;

        for (int i = 0; i < inlets.Length; i++)
        {
            var inlet = inlets[i];
            if (inlet == null) continue;
            float configured = inlet.interval > 0 ? inlet.interval : dropInterval;
            configured += inletIntervalOffsets[i];
            float interval = Mathf.Max(minimumDropInterval, configured / Mathf.Max(.01f, FeedingSpeedMultiplier));
            inlet.Tick(this, interval, Time.deltaTime);
        }
    }

    internal bool TryDropFromInlet(MergeItem material, Vector3 position)
    {
        if (!CanOperate || ActiveItemCount >= maximumActiveItems) return false;
        int before = Revision;
        var piece = SpawnPiece(material, position);
        if (piece == null) return false;
        if (!piece.IsLive || Revision != before || !CanOperate)
        {
            piece.ReturnToPool();
            return false;
        }

        piece.SetSimulated(true);
        return true;
    }

    internal bool TryGetReturnPosition(out Vector3 position)
    {
        var inlet = inlets.Length > 0 ? inlets[inlets.Length / 2] : null;
        if (inlet?.spawnPoint != null)
        {
            position = inlet.spawnPoint.position;
            return true;
        }

        position = default;
        return false;
    }
}
