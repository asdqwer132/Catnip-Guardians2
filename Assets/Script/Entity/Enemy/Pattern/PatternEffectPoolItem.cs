using System.Collections;
using UnityEngine;

public class PatternEffectPoolItem : MonoBehaviour, IPoolable
{
    [Header("Life")]
    [Min(0f)] public float lifeTime = 1f;

    private Coroutine lifeCoroutine;

    public void OnSpawnedFromPool()
    {
        Init();
    }

    public void OnReturnedToPool()
    {
        StopLifeRoutine();
    }

    public void Init()
    {
        StopLifeRoutine();

        float fixedLifeTime = Mathf.Max(0f, lifeTime);
        lifeCoroutine = StartCoroutine(LifeRoutine(fixedLifeTime));
    }

    private IEnumerator LifeRoutine(float fixedLifeTime)
    {
        yield return new WaitForSeconds(fixedLifeTime);

        lifeCoroutine = null;
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        StopLifeRoutine();

        if (ObjectPoolManager.instance != null)
        {
            ObjectPoolManager.instance.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void StopLifeRoutine()
    {
        if (lifeCoroutine == null)
            return;

        StopCoroutine(lifeCoroutine);
        lifeCoroutine = null;
    }

    private void OnDisable()
    {
        StopLifeRoutine();
    }
}