using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public GameObject OriginalPrefab { get; private set; }

    public void SetOriginalPrefab(GameObject prefab)
    {
        OriginalPrefab = prefab;
    }
}