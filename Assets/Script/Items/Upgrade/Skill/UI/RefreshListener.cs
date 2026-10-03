using UnityEngine;

public abstract class RefreshListener : MonoBehaviour
{
    [Header("Refresh")]
    [SerializeField] private RefreshType listenType = RefreshType.All;
    private bool subscribed = false;

    protected virtual void Start()
    {
        Subscribe();
    }

    protected virtual void OnEnable()
    {
        Subscribe();
    }

    protected virtual void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        if (RefreshBroadcaster.Instance == null)
            return;

        RefreshBroadcaster.Instance.OnRefreshRequested += HandleRefresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (RefreshBroadcaster.Instance != null)
        {
            RefreshBroadcaster.Instance.OnRefreshRequested -= HandleRefresh;
        }

        subscribed = false;
    }

    private void HandleRefresh(RefreshType refreshType)
    {
        if ((refreshType & listenType) == 0)
            return;

        Refresh(refreshType);
    }

    protected abstract void Refresh(RefreshType refreshType);
}
