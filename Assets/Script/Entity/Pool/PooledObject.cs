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

    private IPoolable[] cachedPoolables;

    public void SetOriginalPrefab(GameObject prefab)
    {
        OriginalPrefab = prefab;
        CachePoolables();
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