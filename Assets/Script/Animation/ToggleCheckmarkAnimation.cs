using UnityEngine;
using UnityEngine.UI;

public class ToggleCheckmarkAnimation : MonoBehaviour
{
    public Toggle toggle;
    public GameObject slectedImage;
    public Animator checkmarkAnimator;
    public string checkAnimationName = "check";

    void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void Start() { OnToggleChanged(toggle.isOn); }

    void OnToggleChanged(bool isOn)
    {
        if (checkmarkAnimator == null)
            return;
        slectedImage.SetActive(isOn);
        checkmarkAnimator.gameObject.SetActive(isOn);
        if (isOn)
        {
            checkmarkAnimator.Rebind();
            checkmarkAnimator.Update(0f);
            checkmarkAnimator.Play(checkAnimationName, 0, 0f);
        }
    }
}