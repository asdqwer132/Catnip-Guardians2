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
        if (RefreshBroadcaster.Instance != null)
        {
            RefreshBroadcaster.Instance.OnRefreshRequested -= HandleRefresh;
        }
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        if (RefreshBroadcaster.Instance == null)
        {
           // Debug.LogWarning($"{name} RefreshBroadcaster ¾øÀ½");
            return;
        }

        RefreshBroadcaster.Instance.OnRefreshRequested += Refresh;
        subscribed = true;
    }
    private void HandleRefresh(RefreshType refreshType)
    {
        if ((refreshType & listenType) == 0)
            return;

        Refresh(refreshType);
    }

    protected abstract void Refresh(RefreshType refreshType);
}