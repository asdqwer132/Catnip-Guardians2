using UnityEngine;

public class GeneralUIDataReciever : MonoBehaviour
{
   [SerializeField] private ObjectToggleButton toggleButton;

    public void Initialize(GeneralUIDataSender sender)
    {
        toggleButton = sender.GetToggleBtn();
        toggleButton.InitClose();
    }

    public void TryUIDataToggleButton(bool isActive)
    {
        toggleButton.SetObjectActive(isActive);
    }
}