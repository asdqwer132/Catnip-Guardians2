using UnityEngine;
public enum PoolObjectGroup
{
    Default,
    Enemy,
    Effect
}
public class PooledObject : MonoBehaviour
{
    [Header("Pool Group")]
    [SerializeField] private PoolObjectGroup poolGroup = PoolObjectGroup.Default;

    public PoolObjectGroup PoolGroup => poolGroup;
    public GameObject OriginalPrefab { get; private set; }
    public ObjectPoolManager OwnerPool { get; private set; }
    public bool HasPoolOwner { get; private set; }
    public bool IsSpawned { get; private set; }
    public int SpawnId { get; private set; }

    private IPoolable[] cachedPoolables;

    public void SetOriginalPrefab(GameObject prefab)
    {
        OriginalPrefab = prefab;
        CachePoolables();
    }

    public void SetPoolGroup(PoolObjectGroup group)
    {
        poolGroup = group;
    }

    internal void InitializePool(ObjectPoolManager owner, GameObject prefab)
    {
        OwnerPool = owner;
        HasPoolOwner = owner != null;
        IsSpawned = false;
        SpawnId = 0;
        SetOriginalPrefab(prefab);
    }

    internal bool TryBeginSpawn(ObjectPoolManager owner)
    {
        if (IsSpawned || OwnerPool != owner)
            return false;

        IsSpawned = true;
        unchecked
        {
            SpawnId++;
            if (SpawnId == 0)
                SpawnId++;
        }
        return true;
    }

    internal bool TryBeginReturn(ObjectPoolManager owner)
    {
        if (!IsSpawned || OwnerPool != owner)
            return false;

        // 반환 콜백이나 OnDisable에서 재진입해도 큐에 한 번만 들어간다.
        IsSpawned = false;
        return true;
    }

    public IPoolable[] GetPoolables()
    {
        if (cachedPoolables == null)
            CachePoolables();

        return cachedPoolables;
    }

    private void CachePoolables()
    {
        cachedPoolables = GetComponentsInChildren<IPoolable>(true);
    }
}
