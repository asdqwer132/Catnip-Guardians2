using System.Collections;
using UnityEngine;

public abstract class EnemyPatternAction : ScriptableObject
{
    [Header("Action Info")]
    public string actionMemo;

    public virtual void OnPatternStart(EnemyPatternContext context, EnemyPatternEntry pattern) { }
    public virtual void OnPatternEnd(EnemyPatternContext context, EnemyPatternEntry pattern) 
    {
            context.UnLockMove();
    }

    public abstract IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern);
}
