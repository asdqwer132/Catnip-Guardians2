using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 배틀씬 내부 참조와 외부에서 요청할 수 있는 동작을 모읍니다.
/// 매니저는 같은 씬의 Inspector로 연결하고, 다른 씬은 이 Context를 통해 호출합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleSceneContext : MonoBehaviour
{
    [Tooltip("이 Context와 같은 배틀씬의 GameManager를 연결합니다.")]
    [SerializeField] private GameManager gameManager;

    private static readonly List<BattleSceneContext> contexts = new List<BattleSceneContext>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetContexts() => contexts.Clear();

    private void OnEnable()
    {
        if (!contexts.Contains(this)) contexts.Add(this);
    }

    private void OnDisable() => contexts.Remove(this);

    /// <summary>
    /// 지정한 씬에 활성 Context가 정확히 하나 있을 때만 반환합니다.
    /// 다른 배틀씬을 대신 선택하거나 중복 Context 중 하나를 임의로 선택하지 않습니다.
    /// </summary>
    public static bool TryGet(Scene scene, out BattleSceneContext context)
    {
        context = null;
        if (!scene.IsValid() || !scene.isLoaded) return false;
        foreach (var candidate in contexts)
        {
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.gameObject.scene != scene) continue;
            if (context != null) { context = null; return false; }
            context = candidate;
        }
        return context != null;
    }

    /// <summary>기존 GameOver 경로를 실행해 전투 정지와 결과 화면 처리를 함께 수행합니다.</summary>
    public bool TryRequestDefeat()
    {
        if (!isActiveAndEnabled || gameManager == null || !gameManager.isActiveAndEnabled ||
            gameManager.gameObject.scene != gameObject.scene) return false;
        gameManager.GameOver();
        return true;
    }
}
