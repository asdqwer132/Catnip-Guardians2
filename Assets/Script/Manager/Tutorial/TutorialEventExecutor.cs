using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialEventExecutor : MonoBehaviour
{
    public TutorialProgressManager progressManager;
    public void Test()
    {
        Debug.Log("현재 이벤트 : " + progressManager.currentProgress);
        progressManager.IncreaseProgress();
    }

}