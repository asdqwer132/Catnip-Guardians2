using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public enum PlayerStatusList
{
    //weapon
    Arrow = 0,
    BrokeStar  = 1,
    //food
    BlassCrystar = 2,
    //mineral
    MineralCoal = 3,
    MineralGround = 4,
    MineralLight = 5,
    MineralRainbow = 6,
    BlacksmithDelicate = 7,
    BlacksmithResilient = 8,
}
[Serializable]
public class StatusStat : IGameStat<StatusStat>
{
    [Header("Stat")]
    public float Arrow;
    public float BrokeStar;
    public float BlassCrystar;
    public float MineralCoal;
    public float MineralGround;
    public float MineralLight;
    public float MineralRainbow;
    public float BlacksmithResilient;
    public float BlacksmithDelicate;

    public StatusStat Clone()
    {
        return new StatusStat
        {
            Arrow = Arrow,
            BrokeStar = BrokeStar,
            BlassCrystar = BlassCrystar,
            MineralCoal = MineralCoal,
            MineralGround = MineralGround,
            MineralLight = MineralLight,
            MineralRainbow = MineralRainbow,
            BlacksmithResilient = BlacksmithResilient,
            BlacksmithDelicate = BlacksmithDelicate,
        };
    }

    public void Clamp()
    {
        Arrow = Mathf.Max(0f, Arrow);
        BrokeStar = Mathf.Max(0f, BrokeStar);
        BlassCrystar = Mathf.Max(0f, BlassCrystar);
        MineralCoal = Mathf.Max(0f, MineralCoal);
        MineralGround = Mathf.Max(0f, MineralGround);
        MineralLight = Mathf.Max(0f, MineralLight);
        MineralRainbow = Mathf.Max(0f, MineralRainbow);
        BlacksmithResilient = Mathf.Max(0f, BlacksmithResilient);
        BlacksmithDelicate = Mathf.Max(0f, BlacksmithDelicate);
    }
}
[BuffTargetGroups("PlayerStatus")]
public class StatusManager : MonoBehaviour, IBuffTarget, IDynamicBuffReceiver
{
    [Header("Reference")]
    public BuffManager buffManager;
    [Header("Legacy Numeric Status Stats")]
    public StatusStat baseStat = new StatusStat();
    [Tooltip("수치 Modifier가 적용된 최종 스탯입니다. 상태 키만 부여한 효과는 이 수치를 변경하지 않습니다.")]
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
                if (buff != null && !buff.IsExpired && buff.specialStatusKey != null &&
                    buff.specialStatusKey.exposesPlayerStatus && buff.specialStatusKey.playerStatus == playerStatus &&
                    buff.MatchesQuery(query)) return true;
        }

        if (currentStat == null) return false;
        switch (playerStatus)
        {
            case PlayerStatusList.Arrow:
                return currentStat.Arrow > 0f;
            case PlayerStatusList.BrokeStar:
                return currentStat.BrokeStar > 0f;
        }
        return false;
    }

    // 상태 키 판정은 StatusStat 수치/기존 enum 매핑에 의존하지 않는다.
    public bool HasSpecialStatus(SpecialStatusDefinition specialStatusKey)
        => specialStatusKey != null && buffManager != null &&
            buffManager.HasSpecialStatus(specialStatusKey, BuffQueryContext.ForTarget(this));

    public void GetActiveSpecialStatuses(List<ActiveBuff> results)
    {
        if (results == null) return;
        results.Clear();
        if (buffManager == null || buffManager.Storage == null) return;
        BuffQueryContext query = BuffQueryContext.ForTarget(this);
        foreach (ActiveBuff buff in buffManager.Storage.activeBuffs)
            if (buff != null && !buff.IsExpired && buff.specialStatusKey != null && buff.MatchesQuery(query))
                results.Add(buff);
    }


}
