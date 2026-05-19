using UnityEngine;

public abstract class RefreshListener : MonoBehaviour
{
    [Header("Refresh")]
    [SerializeField] private RefreshType listenType = RefreshType.All;

    protected virtual void OnEnable()
    {
        if (RefreshBroadcaster.Instance != null)
        {
            RefreshBroadcaster.Instance.OnRefreshRequested += HandleRefresh;
        }
    }

    protected virtual void OnDisable()
    {
        if (RefreshBroadcaster.Instance != null)
        {
            RefreshBroadcaster.Instance.OnRefreshRequested -= HandleRefresh;
        }
    }

    private void HandleRefresh(RefreshType refreshType)
    {
        if ((refreshType & listenType) == 0)
            return;

        RefreshUI(refreshType);
    }

    protected abstract void RefreshUI(RefreshType refreshType);
}