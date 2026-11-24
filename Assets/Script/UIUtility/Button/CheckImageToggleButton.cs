using UnityEngine;
using UnityEngine.UI;

public class CheckImageToggleButton : MonoBehaviour
{
    public GameObject checkImageObject;
    public bool isChecked = false;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(ToggleCheck);
    }
    void Start() => UpdateCheckImage();

    public void ToggleCheck()
    {
        isChecked = !isChecked;
        UpdateCheckImage();
    }
    public void UpdateCheckImage()
    {
        if (checkImageObject == null)
            return;

        checkImageObject.SetActive(isChecked);
    }
}