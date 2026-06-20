using System.Collections;
using UnityEngine;

public abstract class EnemyPatternAction : ScriptableObject
{
    [Header("Action Info")]
    public string actionMemo;

    public virtual bool ForceBlockDefaultAI => false;

    public virtual void OnPatternStart(EnemyPatternContext context, EnemyPatternEntry pattern) { }
    public virtual void OnPatternEnd(EnemyPatternContext context, EnemyPatternEntry pattern) { }

    public abstract IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern);
}
