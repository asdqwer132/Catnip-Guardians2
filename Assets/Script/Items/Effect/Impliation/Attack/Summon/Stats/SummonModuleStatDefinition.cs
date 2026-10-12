using System;
using UnityEngine;

public abstract class SummonModuleStatDefinition : ScriptableObject
{
    public abstract Type StatType { get; }
}

public abstract class SummonModuleStatDefinition<T> : SummonModuleStatDefinition where T : class, IGameStat<T>, new()
{
    [Header("기본 스탯 · 공유 프로필")]
    public T baseStat = new T();
    public override Type StatType => typeof(T);
    public T CloneBaseStat() => BuffStatCloneUtility.Clone(baseStat ?? new T());
}
