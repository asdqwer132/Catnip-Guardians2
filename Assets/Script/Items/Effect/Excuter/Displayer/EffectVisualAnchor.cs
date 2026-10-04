using UnityEngine;

// 한 번의 연출 재생이 한 앵커+VFX 인스턴스를 소유한다. 다른 적과 공유하지 않는다.
[DisallowMultipleComponent]
public class EffectVisualAnchor : MonoBehaviour, IPoolable
{
    // 복제 시 저장된 참조를 사용하지 않는다. 대여한 루트의 자식만 연결한다.
    private ImpactVfxInstance visualInstance;

    private EffectVisualContext context;
    private PooledObject pooledObject;
    private int spawnId;
    private bool initialized;
    private bool releasing;

    public ImpactVfxInstance Visual
    {
        get
        {
            if (!OwnsVisual(visualInstance))
                ResolveVisual();
            return visualInstance;
        }
    }

    public bool OwnsVisual(ImpactVfxInstance visual)
    {
        return visual != null && visual.transform != transform &&
            visual.transform.IsChildOf(transform);
    }

    public bool IsCurrentPlayback(EffectVisualContext expectedContext)
    {
        return initialized && !releasing && context == expectedContext &&
            (pooledObject == null || !pooledObject.HasPoolOwner ||
                (pooledObject.IsSpawned && pooledObject.SpawnId == spawnId));
    }

    private void Awake()
    {
        ResolveVisual();
        pooledObject = GetComponent<PooledObject>();
    }

    private void ResolveVisual()
    {
        visualInstance = GetComponentInChildren<ImpactVfxInstance>(true);
        if (!OwnsVisual(visualInstance))
            visualInstance = null;
    }

    // 기존 풀 서비스의 호출을 유지하되 외부 인스턴스는 연결하지 않는다.
    public void SetVisualTemplate(ImpactVfxInstance visual)
    {
        visualInstance = OwnsVisual(visual) ? visual : null;
    }

    public void Init(Transform visual, EffectVisualContext context)
    {
        ImpactVfxInstance candidate = visual != null
            ? visual.GetComponent<ImpactVfxInstance>() : null;
        if (OwnsVisual(candidate))
            visualInstance = candidate;
        else
            ResolveVisual();

        this.context = context;
        initialized = context != null && visualInstance != null;
        releasing = false;
        pooledObject = GetComponent<PooledObject>();
        spawnId = pooledObject != null ? pooledObject.SpawnId : 0;
        enabled = true;
    }

    public void OnSpawnedFromPool()
    {
        context = null;
        initialized = false;
        releasing = false;
        ResolveVisual();
        pooledObject = GetComponent<PooledObject>();
        spawnId = pooledObject != null ? pooledObject.SpawnId : 0;
        transform.localScale = Vector3.one;
        enabled = true;
    }

    public void OnReturnedToPool()
    {
        context = null;
        initialized = false;
        releasing = true;
        spawnId = 0;
        transform.localScale = Vector3.one;
    }

    public void ReturnToPool()
    {
        if (releasing)
            return;

        releasing = true;
        initialized = false;
        context = null;

        if (pooledObject != null && pooledObject.HasPoolOwner)
        {
            if (pooledObject.SpawnId != spawnId || !pooledObject.IsSpawned)
                return;

            if (pooledObject.OwnerPool != null)
            {
                if (visualInstance == null)
                    pooledObject.OwnerPool.Discard(gameObject, spawnId);
                else
                    pooledObject.OwnerPool.Release(gameObject, spawnId);
                return;
            }
        }

        // 매니저가 없는 씬 또는 매니저가 이미 제거된 경우의 호환 경로.
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (!initialized || releasing)
            return;

        if (visualInstance == null || !visualInstance.isActiveAndEnabled ||
            context == null || !context.CanContinuePlayback)
        {
            ReturnToPool();
            return;
        }

        // 위치와 크기는 이 앵커의 ImpactVfxInstance가 자기 컨텍스트로 갱신한다.
        // 앵커는 묶음의 유효성 검사와 반환만 담당한다.
    }

    private void OnDisable()
    {
        context = null;
        initialized = false;
    }
}
