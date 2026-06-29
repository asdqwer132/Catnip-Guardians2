using UnityEngine;
using UnityEngine.UI;

public class OffscreenTargetIndicatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;

    [Tooltip("스프라이트를 바꿀 Image. 비워두면 자식에서 자동 탐색")]
    [SerializeField] private Image indicatorImage;

    [Tooltip("비활성화할 실제 화살표 오브젝트. 비워두면 자기 자신을 껐다 켬")]
    [SerializeField] private GameObject visibleRoot;

    public Transform Target { get; private set; }

    private Vector3 defaultEulerAngles;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            return rectTransform;
        }
    }

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (indicatorImage == null)
            indicatorImage = GetComponentInChildren<Image>(true);

        if (visibleRoot == null)
            visibleRoot = gameObject;

        defaultEulerAngles = RectTransform.localEulerAngles;
    }

    public void Init(Transform target)
    {
        Target = target;
    }

    public void SetSprite(Sprite sprite)
    {
        if (indicatorImage == null)
            return;

        if (sprite == null)
            return;

        indicatorImage.sprite = sprite;
    }

    public void SetVisible(bool visible)
    {
        if (visibleRoot != null && visibleRoot.activeSelf != visible)
            visibleRoot.SetActive(visible);
    }

    public void SetPositionAndRotation(Vector2 anchoredPosition, float zAngle, bool useRotation)
    {
        RectTransform.anchoredPosition = anchoredPosition;

        if (useRotation)
        {
            RectTransform.localEulerAngles = new Vector3(0f, 0f, zAngle);
        }
        else
        {
            RectTransform.localEulerAngles = defaultEulerAngles;
        }
    }
}