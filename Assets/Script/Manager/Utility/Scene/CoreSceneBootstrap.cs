using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 빌드의 첫 씬인 CoreScene에 둡니다. 이후 화면은 Additive로 교체하므로 코어는 남습니다.
public sealed class CoreSceneBootstrap : MonoBehaviour
{
    public string startupScene = GameSceneRoutes.Main;
    private IEnumerator Start()
    {
        // 설정/재화 등 코어 매니저들의 Start가 완료된 후 화면을 엽니다.
        yield return null;
        string destination = startupScene;
#if UNITY_EDITOR
        destination = UnityEditor.SessionState.GetString("Catnip.SceneToPlay", destination);
#endif
        destination = GameSceneRoutes.Resolve(destination);
        if (destination == GameSceneRoutes.Core || string.IsNullOrEmpty(destination)) destination = startupScene;
        if (SceneMoveManager.instance != null) SceneMoveManager.instance.LoadScene(destination);
        else Debug.LogError("CoreScene에 SceneMoveManager를 연결하세요.", this);
    }
}
