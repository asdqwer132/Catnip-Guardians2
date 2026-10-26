using System;
using System.Collections;
using UnityEngine;

public class GeneralUIDataSender : MonoBehaviour
{
    [SerializeField] private GeneralUIData uIData;
    public static GeneralUIDataSender instance;
    public static event Action InstanceChanged;
    public bool IsReady { get; private set; }
    private Coroutine readyRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistration()
    {
        instance = null;
        InstanceChanged = null;
    }

    private void OnEnable()
    {
        // UI 참조는 해당 씬과 함께 해제합니다. 다음 배틀 씬의 송신자가 다시 등록됩니다.
        instance = this;
        IsReady = false;
        InstanceChanged?.Invoke();
        readyRoutine = StartCoroutine(PublishReady());
    }

    private IEnumerator PublishReady()
    {
        // ObjectToggleButton.Start의 초기 상태 적용이 끝난 뒤 대기 중인 요청을 전달합니다.
        yield return null;
        readyRoutine = null;
        IsReady = true;
        if (instance == this) InstanceChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (readyRoutine != null) StopCoroutine(readyRoutine);
        readyRoutine = null;
        IsReady = false;
        if (instance != this) return;
        instance = null;
        InstanceChanged?.Invoke();
    }

    public GeneralUIData GetData() => uIData;
    public ObjectToggleButton GetToggleBtn() => uIData?.toggleButton;
    public string GetSenderSceneName() => gameObject.scene.name;
}
