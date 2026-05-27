using System;
using UnityEngine;

/// <summary>
/// EnemySpawnerStat
/// 
/// 역할:
/// - EnemySpawner가 사용하는 스폰 관련 수치를 담는 순수 데이터 클래스.
/// - 버프 계산 대상이므로 IGameStat을 구현한다.
/// - MonoBehaviour가 아니므로 프리팹에 붙이는 컴포넌트가 아니라, EnemySpawner 안에서 값으로 사용한다.
/// 
/// 주요 스탯:
/// - spawnInterval: 적을 몇 초마다 생성할지.
/// - spawnDistance: 타겟으로부터 얼마나 떨어진 위치에 생성할지.
/// 
/// 주의:
/// - spawnInterval은 낮을수록 강한 값이다.
/// - 그래서 최소값을 강제로 제한해서 0초 스폰 같은 사고를 막는다.
/// </summary>
[Serializable]
public class EnemySpawnerStat : IGameStat<EnemySpawnerStat>
{
    [Header("Spawn")]
    public float spawnInterval = 1.5f;
    public float spawnDistance = 8f;

    public EnemySpawnerStat Clone()
    {
        return new EnemySpawnerStat
        {
            spawnInterval = spawnInterval,
            spawnDistance = spawnDistance
        };
    }

    public void Clamp()
    {
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
        spawnDistance = Mathf.Max(0.01f, spawnDistance);
    }
}

/// <summary>
/// EnemySpawnerBuffStat
/// 
/// 역할:
/// - BuffStat 안에서 EnemySpawnerStat에 적용할 변화량을 정의한다.
/// - 더하기 값과 곱하기 값을 분리해서 밸런싱을 쉽게 한다.
/// 
/// 계산 방식:
/// - 최종값 = (기본값 + add) * (1 + multiplier)
/// 
/// 예:
/// - spawnInterval = 2
/// - spawnInterval = -0.5
/// - spawnIntervalM = -0.25
/// => (2 - 0.5) * 0.75 = 1.125초
/// </summary>
[Serializable]
public class EnemySpawnerBuffStat : IBuffStat<EnemySpawnerStat>
{
    [Header("Spawn Interval")]
    public float spawnInterval = 0f;
    public float spawnIntervalM = 0f;

    [Header("Spawn Distance")]
    public float spawnDistance = 0f;
    public float spawnDistanceM = 0f;

    public void ApplyTo(EnemySpawnerStat target)
    {
        if (target == null)
            return;

        target.spawnInterval += spawnInterval;
        target.spawnInterval *= 1f + spawnIntervalM;

        target.spawnDistance += spawnDistance;
        target.spawnDistance *= 1f + spawnDistanceM;

        target.Clamp();
    }
}