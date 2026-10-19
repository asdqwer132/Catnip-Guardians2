using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 제너럴씬 버튼에서 배틀씬의 공개 동작으로 전달하는 중계입니다.
/// 버튼과 이 컴포넌트, Scene Controller는 제너럴씬 안에서만 Inspector로 연결합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleSceneButtonRelay : MonoBehaviour
{
    [Tooltip("같은 제너럴씬의 Controller입니다. 여기에 설정된 Battle Scene을 호출 대상으로 사용합니다.")]
    [SerializeField] private GameGeneralSceneController sceneController;

    // Unity Button의 OnClick에 연결하는 진입점입니다.
    public void RequestDefeat()
    {
        // 클릭 시점에 확인하므로 씬 로드 순서나 재로딩 후의 이전 참조에 의존하지 않습니다.
        if (!TryGetBattle(out var battle))
        {
            Debug.LogWarning("배틀 호출 대상을 확인할 수 없습니다. 같은 씬의 Scene Controller와 대상 배틀씬의 활성 Context 하나를 연결하세요.", this);
            return;
        }
        if (!battle.TryRequestDefeat())
            Debug.LogWarning("배틀 Context에 같은 씬의 활성 GameManager를 연결하세요.", this);
    }

    public bool TryGetBattle(out BattleSceneContext context)
    {
        context = null;
        if (!isActiveAndEnabled || sceneController == null || sceneController.gameObject.scene != gameObject.scene) return false;
        string name = GameSceneRoutes.Resolve(sceneController.battleScene);
        if (string.IsNullOrEmpty(name)) return false;
        return BattleSceneContext.TryGet(SceneManager.GetSceneByName(name), out context);
    }
}
