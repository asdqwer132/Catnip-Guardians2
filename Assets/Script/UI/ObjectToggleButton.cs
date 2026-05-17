using UnityEngine;
using UnityEngine.UI;

public class ObjectToggleButton : MonoBehaviour
{
    [Header("Target")]
    public GameObject targetObject;

    [Header("Button Image")]
    public Image buttonImage;

    [Header("Sprites")]
    public Sprite onSprite;   // 오브젝트가 켜져 있을 때 버튼 이미지
    public Sprite offSprite;  // 오브젝트가 꺼져 있을 때 버튼 이미지

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        button.onClick.AddListener(ToggleObject);
    }

    void Start()
    {
        UpdateButtonSprite();
    }

    public void ToggleObject()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("토글할 오브젝트가 없습니다.");
            return;
        }

        bool nextState = !targetObject.activeSelf;
        targetObject.SetActive(nextState);

        UpdateButtonSprite();
    }

    public void UpdateButtonSprite()
    {
        if (buttonImage == null || targetObject == null)
            return;

        if (targetObject.activeSelf)
        {
            buttonImage.sprite = onSprite;
        }
        else
        {
            buttonImage.sprite = offSprite;
        }
    }

    public void SetObjectActive(bool active)
    {
        if (targetObject == null)
            return;

        targetObject.SetActive(active);
        UpdateButtonSprite();
    }
}