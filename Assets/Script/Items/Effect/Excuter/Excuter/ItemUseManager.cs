using UnityEngine;

public class ItemUseManager : MonoBehaviour
{
    public GameObject owner;
    public ItemUsePositionProvider positionProvider;
    public ItemUseController itemController;
    private GameInputManager boundInput;
    private void Awake()
    {
        if (owner == null) owner = gameObject;
        if (positionProvider == null) positionProvider = GetComponent<ItemUsePositionProvider>();
    }
    public void Init()
    {
        Unbind();
        itemController?.Init();
        boundInput = GameInputManager.instance;
        if (boundInput != null)
        {
            boundInput.OnUseItemPressed += HandleUseInput;
            boundInput.OnResetAllCooldownPressed += ResetAllCooldowns;
        }
    }
    private void OnDisable() => Unbind();
    private void Unbind()
    {
        if (boundInput != null)
        {
            boundInput.OnUseItemPressed -= HandleUseInput;
            boundInput.OnResetAllCooldownPressed -= ResetAllCooldowns;
        }
        boundInput = null;
    }
    public void ResetAllCooldowns() => itemController?.Init();
    public void HandleUseInput()
    {
        if (positionProvider == null) return;
        if (positionProvider.selectPositionOnly) { positionProvider.SelectPointerPosition(); return; }
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (positionProvider.TryGetTargetPosition(out var target)) itemController?.TryUse(positionProvider.GetUseStartPosition(owner), target);
    }
}
