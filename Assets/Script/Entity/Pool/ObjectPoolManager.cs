using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager instance;

    [Header("Parent")]
    public Transform poolParent;

    private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary =
        new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        instance = this;

        if (poolParent == null)
            poolParent = transform;
    }

    public GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation
    )
    {
        if (prefab == null)
            return null;

        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<GameObject>();

        GameObject obj = null;

        Queue<GameObject> pool = poolDictionary[prefab];

        while (pool.Count > 0 && obj == null)
        {
            obj = pool.Dequeue();
        }

        if (obj == null)
        {
            obj = Instantiate(prefab);
            RegisterPooledObject(obj, prefab);
        }

        obj.transform.SetParent(null);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);

        for (int i = 0; i < poolables.Length; i++)
        {
            poolables[i].OnSpawnedFromPool();
        }

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
            poolDictionary[prefab] = new Queue<GameObject>();

        IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);

        for (int i = 0; i < poolables.Length; i++)
        {
            poolables[i].OnReturnedToPool();
        }

        obj.SetActive(false);
        obj.transform.SetParent(poolParent);

        poolDictionary[prefab].Enqueue(obj);
    }

    private void RegisterPooledObject(GameObject obj, GameObject prefab)
    {
        PooledObject pooledObject = obj.GetComponent<PooledObject>();

        if (pooledObject == null)
            pooledObject = obj.AddComponent<PooledObject>();

        pooledObject.SetOriginalPrefab(prefab);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null)
            return;

        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, poolParent);
            RegisterPooledObject(obj, prefab);

            obj.SetActive(false);

            poolDictionary[prefab].Enqueue(obj);
        }
    }
}