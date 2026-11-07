using System.Collections;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AnimatorClipManager clipManager;

    [Header("Random Animation")]
    [SerializeField] private string randomStateName = "Random";
    [SerializeField] private float interval = 3f;
    [SerializeField] private float randomOffset = 1f;
    [SerializeField] private bool playOnStart = true;

    private Coroutine randomRoutine;

    private void Awake()
    {
        if (clipManager == null)
            clipManager = GetComponent<AnimatorClipManager>();
    }

    public void PlayOnStart()
    {
        if (playOnStart)
            StartRandom();
    }

    // 특정 애니메이션 실행
    public void Play(string stateName)
    {
        clipManager.Play(stateName);
    }

    // 특정 클립으로 교체 후 실행
    public void Play(string stateName, int index)
    {
        clipManager.Play(stateName, index);
    }

    // 해당 State의 랜덤 클립 실행
    public void PlayRandom(string stateName)
    {
        clipManager.PlayRandom(stateName);
    }

    // 설정된 랜덤 애니메이션 실행
    public void PlayRandom()
    {
        PlayRandom(randomStateName);
    }

    // 랜덤 애니메이션 반복 시작
    public void StartRandom()
    {
        StopRandom();
        PlayRandom();
        randomRoutine = StartCoroutine(RandomRoutine());
    }

    // 랜덤 애니메이션 반복 중지
    public void StopRandom()
    {
        if (randomRoutine == null) return;

        StopCoroutine(randomRoutine);
        randomRoutine = null;
    }

    private IEnumerator RandomRoutine()
    {
        while (true)
        {
            float delay = Mathf.Max(
                0.01f,
                interval + Random.Range(-randomOffset, randomOffset)
            );

            yield return new WaitForSeconds(delay);

            PlayRandom();
        }
    }

    // 전체 클립 랜덤 교체
    public void RandomizeAll()
    {
        clipManager.RandomizeAll();
    }

    // 전체 클립 원본 복원
    public void ResetAll()
    {
        clipManager.ResetAllClips();
    }

    private void OnDisable()
    {
        StopRandom();
    }
}