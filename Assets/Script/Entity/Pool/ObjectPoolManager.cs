using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager instance;

    [Header("Default Parent")]
    public Transform poolParent;

    [Header("Active Parents")]
    public Transform enemyActiveParent;
    public Transform effectActiveParent;

    [Header("Inactive Pool Parents")]
    public Transform enemyPoolParent;
    public Transform effectPoolParent;

    private readonly Dictionary<GameObject, Queue<PooledObject>> poolDictionary =
        new Dictionary<GameObject, Queue<PooledObject>>();

    private Transform creationParent;

    private void Awake()
    {
        instance = this;

        if (poolParent == null)
            poolParent = transform;

        if (enemyPoolParent == null)
            enemyPoolParent = poolParent;

        if (effectPoolParent == null)
            effectPoolParent = poolParent;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        PooledObject pooledObject = null;
        Queue<PooledObject> pool = GetPool(prefab);

        while (pool.Count > 0)
        {
            PooledObject candidate = pool.Dequeue();
            if (candidate == null || candidate.IsSpawned || candidate.OwnerPool != this)
                continue;
            pooledObject = candidate;
            break;
        }

        if (pooledObject == null)
        {
            pooledObject = CreateNewObject(prefab);
        }

        if (pooledObject == null || !pooledObject.TryBeginSpawn(this))
            return null;

        int spawnId = pooledObject.SpawnId;
        GameObject obj = pooledObject.gameObject;

        Transform activeParent = GetActiveParent(pooledObject.PoolGroup);

        obj.transform.SetParent(activeParent, false);
        obj.transform.localScale = prefab.transform.localScale;
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        IPoolable[] poolables = pooledObject.GetPoolables();

        for (int i = 0; i < poolables.Length; i++)
        {
            if (!IsCurrentSpawn(pooledObject, spawnId))
                return null;
            try
            {
                poolables[i].OnSpawnedFromPool();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, obj);
            }
        }

        return IsCurrentSpawn(pooledObject, spawnId) ? obj : null;
    }

    public void Release(GameObject obj)
    {
        ReleaseInternal(obj, false, 0);
    }

    // 수명 콜백이 이전 사용분을 반환하려고 할 때 새 사용분을 건드리지 않게 한다.
    public void Release(GameObject obj, int expectedSpawnId)
    {
        ReleaseInternal(obj, true, expectedSpawnId);
    }

    // 자식 VFX가 외부 코드에서 파괴된 묶음은 재사용하지 않는다.
    public void Discard(GameObject obj, int expectedSpawnId)
    {
        PooledObject pooledObject = FindManagedRoot(obj);
        if (pooledObject == null)
        {
            if (obj != null)
                Destroy(obj);
            return;
        }
        if (pooledObject.SpawnId != expectedSpawnId || !pooledObject.IsSpawned)
            return;
        if (pooledObject.OwnerPool != null && pooledObject.OwnerPool != this)
        {
            pooledObject.OwnerPool.Discard(pooledObject.gameObject, expectedSpawnId);
            return;
        }
        pooledObject.TryBeginReturn(this);
        pooledObject.gameObject.SetActive(false);
        Destroy(pooledObject.gameObject);
    }

    private void ReleaseInternal(GameObject obj, bool checkSpawnId, int expectedSpawnId)
    {
        if (obj == null)
            return;

        // VFX의 자식에서 Release를 호출해도 실제로 풀링한 루트를 반환한다.
        PooledObject pooledObject = FindManagedRoot(obj);

        if (pooledObject == null)
        {
            Destroy(obj);
            return;
        }

        if (checkSpawnId && pooledObject.SpawnId != expectedSpawnId)
            return;

        if (pooledObject.OwnerPool != null && pooledObject.OwnerPool != this)
        {
            pooledObject.OwnerPool.ReleaseInternal(pooledObject.gameObject, checkSpawnId, expectedSpawnId);
            return;
        }

        if (pooledObject.OwnerPool == null || pooledObject.OriginalPrefab == null)
        {
            Destroy(pooledObject.gameObject);
            return;
        }

        if (!pooledObject.TryBeginReturn(this))
            return;

        obj = pooledObject.gameObject;
        GameObject prefab = pooledObject.OriginalPrefab;

        IPoolable[] poolables = pooledObject.GetPoolables();

        for (int i = 0; i < poolables.Length; i++)
        {
            try
            {
                poolables[i].OnReturnedToPool();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, obj);
            }
        }

        if (pooledObject == null || obj == null)
            return;

        obj.SetActive(false);

        if (prefab == null)
        {
            Destroy(obj);
            return;
        }

        Transform inactiveParent = GetInactiveParent(pooledObject.PoolGroup);
        obj.transform.SetParent(inactiveParent, false);
        obj.transform.localScale = prefab.transform.localScale;

        GetPool(prefab).Enqueue(pooledObject);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        Queue<PooledObject> pool = GetPool(prefab);
        while (pool.Count < count)
        {
            PooledObject pooledObject = CreateNewObject(prefab);
            pooledObject.transform.SetParent(GetInactiveParent(pooledObject.PoolGroup), false);
            pool.Enqueue(pooledObject);
        }
    }

    private Queue<PooledObject> GetPool(GameObject prefab)
    {
        Queue<PooledObject> pool;
        if (!poolDictionary.TryGetValue(prefab, out pool))
        {
            pool = new Queue<PooledObject>();
            poolDictionary.Add(prefab, pool);
        }
        return pool;
    }

    private bool IsCurrentSpawn(PooledObject pooledObject, int spawnId)
    {
        return pooledObject != null && pooledObject.OwnerPool == this &&
            pooledObject.IsSpawned && pooledObject.SpawnId == spawnId &&
            pooledObject.gameObject.activeSelf;
    }

    public static PooledObject FindManagedRoot(GameObject obj)
    {
        if (obj == null)
            return null;

        for (Transform current = obj.transform; current != null; current = current.parent)
        {
            PooledObject pooledObject = current.GetComponent<PooledObject>();
            if (pooledObject != null && pooledObject.HasPoolOwner)
                return pooledObject;
        }
        return null;
    }

    private PooledObject CreateNewObject(GameObject prefab)
    {
        // 새 객체도 비활성 부모 아래서 생성해 풀 정보 등록 전에 OnEnable이 실행되지 않게 한다.
        if (creationParent == null)
        {
            GameObject stagingObject = new GameObject("PoolCreation");
            stagingObject.SetActive(false);
            stagingObject.transform.SetParent(transform, false);
            creationParent = stagingObject.transform;
        }

        GameObject obj = Instantiate(prefab, creationParent);
        obj.SetActive(false);

        PooledObject pooledObject = obj.GetComponent<PooledObject>();

        if (pooledObject == null)
            pooledObject = obj.AddComponent<PooledObject>();

        pooledObject.InitializePool(this, prefab);

        return pooledObject;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private Transform GetActiveParent(PoolObjectGroup group)
    {
        switch (group)
        {
            case PoolObjectGroup.Enemy:
                return enemyActiveParent;

            case PoolObjectGroup.Effect:
                return effectActiveParent;

            default:
                return null;
        }
    }

    private Transform GetInactiveParent(PoolObjectGroup group)
    {
        switch (group)
        {
            case PoolObjectGroup.Enemy:
                return enemyPoolParent != null ? enemyPoolParent : poolParent;

            case PoolObjectGroup.Effect:
                return effectPoolParent != null ? effectPoolParent : poolParent;

            default:
                return poolParent;
        }
    }
}
