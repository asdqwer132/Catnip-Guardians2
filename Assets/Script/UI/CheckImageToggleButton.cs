using UnityEngine;
using UnityEngine.UI;

public class CheckImageToggleButton : MonoBehaviour
{
    [Header("Check Image")]
    public GameObject checkImageObject;

    [Header("Default State")]
    public bool isChecked = false;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(ToggleCheck);
    }

    void Start()
    {
        UpdateCheckImage();
    }

    public void ToggleCheck()
    {
        isChecked = !isChecked;
        UpdateCheckImage();
    }

    public void SetChecked(bool checkedState)
    {
        isChecked = checkedState;
        UpdateCheckImage();
    }

    public void UpdateCheckImage()
    {
        if (checkImageObject == null)
        {
            Debug.LogWarning("체크 이미지 오브젝트가 없습니다.");
            return;
        }

        checkImageObject.SetActive(isChecked);
    }
}