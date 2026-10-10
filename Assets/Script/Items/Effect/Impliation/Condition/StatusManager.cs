using System;
using Unity.VisualScripting;
using UnityEngine;
public enum PlayerStatusList
{
    arrow = 0,
    star  = 1,
}
[Serializable]
public class StatusStat : IGameStat<StatusStat>
{
    [Header("Stat")]
    public float statArrow;
    public float statStar;

    public StatusStat Clone()
    {
        return new StatusStat
        {
            statArrow = statArrow,
            statStar = statStar
        };
    }

    public void Clamp()
    {
        statArrow = Mathf.Max(0f, statArrow);
        statStar = Mathf.Max(0f, statStar);
    }
}
[BuffTargetGroups("PlayerStatus")]
public class StatusManager : MonoBehaviour, IBuffTarget, IDynamicBuffReceiver
{
    [Header("Reference")]
    public BuffManager buffManager;
    [Header("Stat")]
    public StatusStat baseStat = new StatusStat();
    public StatusStat currentStat = new StatusStat();

    public static StatusManager Instance { get; private set; }

    public UnityEngine.Object BuffTargetObject => this;

    public string BuffTargetGroup => "PlayerStatus";

    public string BuffTargetDebugName => name;
    private void OnEnable()
    {
        RegisterToBuffManager();
    }

    private void OnDisable()
    {
        UnregisterFromBuffManager();
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


    public void OnDynamicBuffChanged()
    {
        RefreshBuffedStat();
    }

    public void RefreshBuffedStat()
    {
        if (baseStat == null)
            return;

        if (buffManager == null)
            currentStat = baseStat.Clone();
        else
        {
            StatusStat buffedStat = buffManager.GetBuffedStatForTarget(baseStat, this);
            currentStat = buffedStat != null ? buffedStat : baseStat.Clone();
        }

        currentStat.Clamp();
    }

    private void Awake()
    {
        Instance = this;
        RefreshBuffedStat();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool HasStatus(PlayerStatusList playerStatus)
    {
        if (buffManager != null && buffManager.Storage != null)
        {
            BuffQueryContext query = BuffQueryContext.ForTarget(this);
            foreach (ActiveBuff buff in buffManager.Storage.activeBuffs)
                if (buff != null && !buff.IsExpired && buff.statusDefinition != null &&
                    buff.statusDefinition.exposesPlayerStatus && buff.statusDefinition.playerStatus == playerStatus &&
                    buff.MatchesQuery(query)) return true;
        }

        if (currentStat == null) return false;
        switch (playerStatus)
        {
            case PlayerStatusList.arrow:
                return currentStat.statArrow > 0f;
            case PlayerStatusList.star:
                return currentStat.statStar > 0f;
        }
        return false;
    }


}
