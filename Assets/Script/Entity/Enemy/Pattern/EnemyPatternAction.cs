using System.Collections;
using UnityEngine;

public abstract class EnemyPatternAction : ScriptableObject
{
    [Header("Action Info")]
    public string actionMemo;


    [Header("Effect")]
    public string animationByName;
    public AudioClipNameWithCategory audioByName;
    public GameObject beforeEffectPrefab;
    public GameObject afterEffectPrefab;

    public virtual bool ForceBlockDefaultAI => false;

    public virtual void OnPatternStart(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context != null &&
            context.Visual != null &&
            !string.IsNullOrEmpty(animationByName))
        {
            context.Visual.PlayAnimationByName(animationByName);
        }

        PlayEffect(beforeEffectPrefab, context.Position);
        if(AudioManager.instance != null && !string.IsNullOrEmpty(audioByName.categoryName) && !string.IsNullOrEmpty(audioByName.clipName))
            AudioManager.instance.PlaySfx(audioByName.categoryName, audioByName.clipName);
    }

    public virtual void OnPatternEnd(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        PlayEffect(afterEffectPrefab, context.Position);
    }
    // 중단 정리는 이 훅 또는 Execute의 try/finally에서 처리한다.
    public virtual void OnPatternInterrupted(EnemyPatternContext context, EnemyPatternEntry pattern) { }

    private void PlayEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        GameObject effect;

        if (ObjectPoolManager.instance != null)
        {
            effect = ObjectPoolManager.instance.Spawn(
                prefab,
                position,
                Quaternion.identity
            );
        }
        else
        {
            // 풀 매니저 없을 때 예비용
            effect = Instantiate(prefab, position, Quaternion.identity);
        }

        if (effect != null && effect.TryGetComponent(out PatternEffectPoolItem poolItem))
        {
            poolItem.Init();
        }
    }

    public abstract IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern);
}