using System;
using UnityEngine;

public class RefreshBroadcaster : MonoBehaviour
{
    public static RefreshBroadcaster Instance { get; private set; }

    public event Action<RefreshType> OnRefreshRequested;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private void Awake()
    {
        Instance = this;
    }

    public void Broadcast(RefreshType refreshType)
    {
        if (refreshType == RefreshType.None)
            return;

        if (debugLog)
            Debug.Log("Refresh Broadcast: " + refreshType);

        OnRefreshRequested?.Invoke(refreshType);
    }
}