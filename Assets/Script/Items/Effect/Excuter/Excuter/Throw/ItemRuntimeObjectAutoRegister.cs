using UnityEngine;

public class ItemRuntimeObjectAutoRegister : MonoBehaviour
{
    [SerializeField] private bool registerOnEnable = true;

    private bool registered;

    private void OnEnable()
    {
        if (!registerOnEnable)
            return;

        Register();
    }

    public void Register()
    {
        if (registered)
            return;

        if (ItemRuntimeObjectManager.Instance == null)
            return;

        ItemRuntimeObjectManager.Instance.Register(gameObject);
        registered = true;
    }
}