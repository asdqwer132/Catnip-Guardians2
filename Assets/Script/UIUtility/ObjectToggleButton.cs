using UnityEngine;
using UnityEngine.UI;

public class ObjectToggleButton : MonoBehaviour
{
    [Header("Targets")]
    public GameObject[] targetObjects;
    public bool initClose = true;

    [Header("Button Image")]
    public Image buttonImage;

    [Header("Sprites")]
    public Sprite onSprite;
    public Sprite offSprite;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (buttonImage != null)
        {
            if (onSprite == null)
                onSprite = buttonImage.sprite;

            if (offSprite == null)
                offSprite = buttonImage.sprite;
        }

        if (button != null)
            button.onClick.AddListener(ToggleObject);
    }

    private void Start()
    {
        SetObjectActive(!initClose);
    }

    public void ToggleObject()
    {
        bool currentActive = HasAnyActiveTarget();
        SetObjectActive(!currentActive);
    }

    private bool HasAnyActiveTarget()
    {
        if (targetObjects == null || targetObjects.Length == 0)
            return false;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null && targetObjects[i].activeSelf)
                return true;
        }

        return false;
    }

    public void SetObjectActive(bool active)
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogWarning("토글할 오브젝트가 없습니다.");
            UpdateButtonSprite();
            return;
        }

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null)
                targetObjects[i].SetActive(active);
        }

        UpdateButtonSprite();
    }

    public void UpdateButtonSprite()
    {
        if (buttonImage == null)
            return;

        bool currentActive = HasAnyActiveTarget();
        buttonImage.sprite = currentActive ? onSprite : offSprite;
    }
}