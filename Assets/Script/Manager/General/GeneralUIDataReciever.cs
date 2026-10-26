using UnityEngine;

public class GeneralUIDataReciever : MonoBehaviour
{
    [SerializeField] private ObjectToggleButton toggleButton;
    private GeneralUIDataSender boundSender;
    private bool? pendingActive;

    private void OnEnable()
    {
        GeneralUIDataSender.InstanceChanged += BindCurrentSender;
        BindCurrentSender();
    }

    private void OnDisable()
    {
        GeneralUIDataSender.InstanceChanged -= BindCurrentSender;
        boundSender = null;
        toggleButton = null;
    }

    private void BindCurrentSender() => Initialize(GeneralUIDataSender.instance);

    public void Initialize(GeneralUIDataSender sender)
    {
        boundSender = sender;
        toggleButton = sender != null ? sender.GetToggleBtn() : null;
        if (!CanApply || !pendingActive.HasValue) return;
        bool active = pendingActive.Value;
        pendingActive = null;
        toggleButton.SetObjectActive(active);
    }

    private bool CanApply => boundSender != null && boundSender.isActiveAndEnabled &&
        boundSender.IsReady && toggleButton != null;

    public void TryUIDataToggleButton(bool isActive)
    {
        // 송신 씬이 아직 없으면 마지막 요청을 보관합니다.
        pendingActive = isActive;
        BindCurrentSender();
    }
}
