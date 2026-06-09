using System.Collections.Generic;
using UnityEngine;

public abstract class AttackObjectBase : MonoBehaviour
{
    private static readonly List<AttackObjectBase> activeEntities =
        new List<AttackObjectBase>();

    protected virtual void OnEnable()
    {
        if (!activeEntities.Contains(this))
            activeEntities.Add(this);
    }

    protected virtual void OnDisable()
    {
        activeEntities.Remove(this);
        UnregisterDynamicBuffReceiverInternal();
    }

    protected abstract void UnregisterDynamicBuffReceiverInternal();

    public virtual void Clear()
    {
        UnregisterDynamicBuffReceiverInternal();

        if (gameObject == null)
            return;

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public static void ClearAllActiveEntities()
    {
        for (int i = activeEntities.Count - 1; i >= 0; i--)
        {
            AttackObjectBase entity = activeEntities[i];

            if (entity == null)
            {
                activeEntities.RemoveAt(i);
                continue;
            }

            entity.Clear();
        }

        activeEntities.Clear();
    }

    public static void ClearAllActiveAreas()
    {
        ClearAllActiveEntities();
    }
}