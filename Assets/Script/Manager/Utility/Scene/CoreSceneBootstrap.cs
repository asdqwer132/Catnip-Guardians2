using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 빌드의 첫 씬인 CoreScene에 둡니다. 이후 화면은 Additive로 교체하므로 코어는 남습니다.
public sealed class CoreSceneBootstrap : MonoBehaviour
{
    public bool autoLoadStartupScene = true;
    public string startupScene = GameSceneRoutes.General;
    [Tooltip("코어를 다른 테스트 씬에 추가로 열었을 때 자동 이동하지 않습니다.")]
    public bool onlyWhenCoreIsActive = true;
    private IEnumerator Start()
    {
        // 설정/재화 등 코어 매니저들의 Start가 완료된 후 화면을 엽니다.
        yield return null;
        string editorTarget = "";
#if UNITY_EDITOR
        editorTarget = UnityEditor.SessionState.GetString("Catnip.SceneToPlay", "");
#endif
        string destination = SceneStartupPolicy.CoreDestination(startupScene, autoLoadStartupScene,
            SceneManager.GetActiveScene() == gameObject.scene, onlyWhenCoreIsActive, editorTarget);
        if (string.IsNullOrEmpty(destination)) yield break;
        if (SceneMoveManager.instance != null) SceneMoveManager.instance.LoadScene(destination);
        else Debug.LogError("CoreScene에 SceneMoveManager를 연결하세요.", this);
    }
}
