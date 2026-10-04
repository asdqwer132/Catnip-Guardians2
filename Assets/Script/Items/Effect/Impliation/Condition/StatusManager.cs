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
    }

    public bool HasStatus(PlayerStatusList playerStatus)
    {
        bool hasStatus = false;
        switch (playerStatus)
        {
            case PlayerStatusList.arrow:
                if (currentStat.statArrow == 1) hasStatus = true;
                break;
            case PlayerStatusList.star:
                if (currentStat.statStar == 1) hasStatus = true;
                break;
        }
        return hasStatus;
    }


}
