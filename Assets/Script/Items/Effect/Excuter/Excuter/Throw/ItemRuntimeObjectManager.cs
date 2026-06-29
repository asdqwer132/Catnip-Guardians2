using System.Collections.Generic;
using UnityEngine;

public class ItemRuntimeObjectManager : MonoBehaviour
{
    public static ItemRuntimeObjectManager Instance { get; private set; }

    private readonly HashSet<GameObject> runtimeObjects = new HashSet<GameObject>();
    private readonly List<GameObject> removeBuffer = new List<GameObject>();

    private bool isClearing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Register(GameObject obj)
    {
        if (obj == null)
            return;

        CleanupNullObjects();

        runtimeObjects.Add(obj);

        ItemRuntimeObjectTracker tracker = obj.GetComponent<ItemRuntimeObjectTracker>();
        if (tracker == null)
            tracker = obj.AddComponent<ItemRuntimeObjectTracker>();

        tracker.Init(this);
    }

    public void Register(Component component)
    {
        if (component == null)
            return;

        Register(component.gameObject);
    }

    public void Unregister(GameObject obj)
    {
        if (obj == null)
            return;

        if (isClearing)
            return;

        runtimeObjects.Remove(obj);
    }

    public void ClearAll()
    {
        isClearing = true;

        Debug.Log(runtimeObjects.Count + "cleared");
        foreach (GameObject obj in runtimeObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        runtimeObjects.Clear();
        isClearing = false;
    }

    private void CleanupNullObjects()
    {
        removeBuffer.Clear();

        foreach (GameObject obj in runtimeObjects)
        {
            if (obj == null)
                removeBuffer.Add(obj);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            runtimeObjects.Remove(removeBuffer[i]);
        }
    }
}