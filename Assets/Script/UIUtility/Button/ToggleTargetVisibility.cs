using UnityEngine;

public class ToggleTargetVisibility : MonoBehaviour
{
    [SerializeField] private GameObject target;

    private void Awake()
    {
        if (target == null)
            target = gameObject;
    }
    public void OnToggleChanged(bool isOn)
    {
        target.SetActive(!isOn);
    }
}
