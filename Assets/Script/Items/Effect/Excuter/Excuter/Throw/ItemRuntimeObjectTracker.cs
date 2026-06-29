using UnityEngine;

public class ItemRuntimeObjectTracker : MonoBehaviour
{
    private ItemRuntimeObjectManager manager;

    public void Init(ItemRuntimeObjectManager ownerManager)
    {
        manager = ownerManager;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.Unregister(gameObject);
    }
}