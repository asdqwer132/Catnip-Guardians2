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

        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<PooledObject>();

        PooledObject pooledObject = null;
        Queue<PooledObject> pool = poolDictionary[prefab];

        while (pool.Count > 0 && pooledObject == null)
            pooledObject = pool.Dequeue();

        if (pooledObject == null)
        {
            pooledObject = CreateNewObject(prefab);
        }

        GameObject obj = pooledObject.gameObject;

        Transform activeParent = GetActiveParent(pooledObject.PoolGroup);

        obj.transform.SetParent(activeParent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        IPoolable[] poolables = pooledObject.GetPoolables();

        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnSpawnedFromPool();

        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null)
            return;

        PooledObject pooledObject = obj.GetComponent<PooledObject>();

        if (pooledObject == null || pooledObject.OriginalPrefab == null)
        {
            Destroy(obj);
            return;
        }

        GameObject prefab = pooledObject.OriginalPrefab;

        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<PooledObject>();

        IPoolable[] poolables = pooledObject.GetPoolables();

        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnReturnedToPool();

        obj.SetActive(false);

        Transform inactiveParent = GetInactiveParent(pooledObject.PoolGroup);
        obj.transform.SetParent(inactiveParent);

        poolDictionary[prefab].Enqueue(pooledObject);
    }

    private PooledObject CreateNewObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);

        PooledObject pooledObject = obj.GetComponent<PooledObject>();

        if (pooledObject == null)
            pooledObject = obj.AddComponent<PooledObject>();

        pooledObject.SetOriginalPrefab(prefab);

        return pooledObject;
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