using UnityEngine;
using UnityEngine.UI;

public class ObjectToggleButton : MonoBehaviour
{
    [Header("Targets")]
    public GameObject[] targetObjects;
    public bool initClose = true;

    [Header("Close Targets When Open")]
    public GameObject[] closeTargetsWhenOpen;

    [Header("UI")]
    public Image buttonImage;
    public Button button;
    public Toggle toggle;

    [Header("Sprites")]
    public Sprite onSprite;
    public Sprite offSprite;

    private bool isChanging;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (toggle == null)
            toggle = GetComponent<Toggle>();

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

        if (toggle != null)
            toggle.onValueChanged.AddListener(SetObjectActive);
    }

    private void Start()
    {
        SetObjectActive(!initClose);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ToggleObject);

        if (toggle != null)
            toggle.onValueChanged.RemoveListener(SetObjectActive);
    }

    public void ToggleObject()
    {
        bool currentActive = HasAnyActiveTarget();
        SetObjectActive(!currentActive);
    }

    public void SetObjectActive(bool active)
    {
        if (isChanging)
            return;

        isChanging = true;

        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogWarning("토글할 오브젝트가 없습니다." + name);
            UpdateUIState();
            isChanging = false;
            return;
        }

        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null)
                targetObjects[i].SetActive(active);
        }

        if (active)
            CloseOtherTargets();

        UpdateUIState();

        isChanging = false;
    }

    private void CloseOtherTargets()
    {
        if (closeTargetsWhenOpen == null)
            return;

        for (int i = 0; i < closeTargetsWhenOpen.Length; i++)
        {
            if (closeTargetsWhenOpen[i] != null)
                closeTargetsWhenOpen[i].SetActive(false);
        }
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

    public void UpdateUIState()
    {
        bool currentActive = HasAnyActiveTarget();

        if (buttonImage != null)
            buttonImage.sprite = currentActive ? onSprite : offSprite;

        if (toggle != null)
            toggle.SetIsOnWithoutNotify(currentActive);
    }
}