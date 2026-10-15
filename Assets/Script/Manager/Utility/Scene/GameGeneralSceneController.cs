using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// UI를 만들거나 옮기지 않습니다. 기존 씬 카메라와 Canvas의 표시 영역만 연결합니다.
public sealed class GameGeneralSceneController : MonoBehaviour
{
    public string battleScene = GameSceneRoutes.Battle;
    public string itemScene = GameSceneRoutes.Item;
    [Tooltip("켜면 제너럴 씬이 배틀/아이템 씬을 함께 엽니다. 개별 씬 실행에는 영향을 주지 않습니다.")]
    public bool autoLoadChildren = true;
    [Header("화면 분할")]
    public GameScreenLayout layout;
    [Range(0.1f, 0.9f)] public float battleRatio = 0.5f;
    public bool battleFirst = true;
    [Range(0f, 0.1f)] public float gap;
    [Tooltip("기존 Screen Space Overlay Canvas를 해당 씬의 카메라에 연결합니다. 코어 UI는 전체 화면으로 유지합니다.")]
    public bool fitSceneCanvases = true;
    [Header("서브 게임 격리")]
    [Range(8, 31)] public int itemLayer = 31;
    [Tooltip("아이템 씬만 별도의 2D 물리 공간에서 실행합니다.")]
    public bool isolateItemPhysics = true;
    public Camera BattleCamera { get; private set; }
    public Camera ItemCamera { get; private set; }
    public Scene ItemScene => item;
    public PhysicsScene2D ItemPhysics => itemPhysics;
    private bool loading, ready;
    private Scene battle, item;
    private PhysicsScene2D itemPhysics;
    private readonly List<CanvasState> canvases = new List<CanvasState>();
    private sealed class CanvasState
    {
        public Canvas canvas; public RenderMode mode; public Camera camera; public float distance;
    }
    private IEnumerator Start()
    {
        if (autoLoadChildren && (SceneMoveManager.instance == null || !SceneMoveManager.instance.IsLoading)) yield return LoadChildren();
    }
    public IEnumerator LoadChildren()
    {
        while (loading) yield return null;
        if (ready) yield break;
        if (battleScene == itemScene || battleScene == GameSceneRoutes.Core || itemScene == GameSceneRoutes.Core ||
            battleScene == gameObject.scene.name || itemScene == gameObject.scene.name ||
            !Application.CanStreamedLevelBeLoaded(battleScene) || !Application.CanStreamedLevelBeLoaded(itemScene))
        { Debug.LogError("GameGeneralScene: 서로 다른 Battle/Item 씬을 빌드 목록에 등록하세요.", this); yield break; }
        loading = true;
        try
        {
            battle = SceneManager.GetSceneByName(battleScene);
            if (!battle.isLoaded) yield return SceneManager.LoadSceneAsync(battleScene, LoadSceneMode.Additive);
            battle = SceneManager.GetSceneByName(battleScene);
            item = SceneManager.GetSceneByName(itemScene);
            if (!item.isLoaded)
                yield return SceneManager.LoadSceneAsync(itemScene, new LoadSceneParameters(LoadSceneMode.Additive,
                    isolateItemPhysics ? LocalPhysicsMode.Physics2D : LocalPhysicsMode.None));
            item = SceneManager.GetSceneByName(itemScene);
            if (!battle.isLoaded || !item.isLoaded) yield break;
            itemPhysics = item.GetPhysicsScene2D();
            BattleCamera = FindCamera(battle); ItemCamera = FindCamera(item);
            if (BattleCamera == null || ItemCamera == null)
            { Debug.LogError("배틀 씬과 아이템 씬에 Camera를 각각 연결하세요.", this); yield break; }
            // 전투 레이어는 보존하고, 서브 게임의 월드 오브젝트만 전용 레이어로 분리합니다.
            foreach (var root in item.GetRootGameObjects()) SetItemLayer(root.transform);
            BattleCamera.cullingMask &= ~(1 << itemLayer); ItemCamera.cullingMask = 1 << itemLayer;
            ItemCamera.tag = "Untagged";
            foreach (var listener in ItemCamera.GetComponents<AudioListener>()) listener.enabled = false;
            if (fitSceneCanvases) { ConnectCanvases(battle, BattleCamera); ConnectCanvases(item, ItemCamera); }
            ready = true; ApplyLayout();
        }
        finally { loading = false; }
    }
    private static Camera FindCamera(Scene scene)
    {
        Camera fallback = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (!camera.enabled || !camera.gameObject.activeInHierarchy) continue;
                if (camera.CompareTag("MainCamera")) return camera;
                if (fallback == null) fallback = camera;
            }
        return fallback;
    }
    private void SetItemLayer(Transform node)
    {
        // 아이템 씬의 UI도 전용 레이어에 두어 배틀 카메라에 섞이지 않게 합니다.
        node.gameObject.layer = itemLayer;
        foreach (Transform child in node) SetItemLayer(child);
    }
    // 런타임에 생성하는 서브 게임 객체도 이 함수로 씬/레이어를 연결할 수 있습니다.
    public void RegisterItemObject(GameObject value)
    {
        if (value == null || !item.isLoaded) return;
        if (value.scene != item)
        {
            value.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(value, item);
        }
        SetItemLayer(value.transform);
    }
    private void ConnectCanvases(Scene scene, Camera camera)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                canvases.Add(new CanvasState { canvas = canvas, mode = canvas.renderMode, camera = canvas.worldCamera, distance = canvas.planeDistance });
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Clamp(canvas.planeDistance, camera.nearClipPlane + 0.1f, camera.farClipPlane - 0.1f);
            }
    }
    public void SetBattleRatio(float value) { battleRatio = value; ApplyLayout(); }
    public void SetLayout(int value) { if (value >= 0 && value <= 3) layout = (GameScreenLayout)value; ApplyLayout(); }
    public void ApplyLayout()
    {
        if (!ready) return;
        GameViewportLayout.Calculate(layout, battleRatio, battleFirst, gap, out var battleRect, out var itemRect);
        ApplyCamera(BattleCamera, battleRect); ApplyCamera(ItemCamera, itemRect);
    }
    private static void ApplyCamera(Camera camera, Rect rect)
    {
        if (camera == null) return;
        camera.enabled = rect.width > 0 && rect.height > 0;
        if (camera.enabled) camera.rect = rect;
    }
    private void LateUpdate() => ApplyLayout();
    private void FixedUpdate()
    {
        if (ready && isolateItemPhysics && itemPhysics.IsValid() && itemPhysics != Physics2D.defaultPhysicsScene && Time.timeScale > 0f)
            itemPhysics.Simulate(Time.fixedDeltaTime);
    }
    private void OnDestroy()
    {
        foreach (var state in canvases)
            if (state.canvas != null)
            { state.canvas.renderMode = state.mode; state.canvas.worldCamera = state.camera; state.canvas.planeDistance = state.distance; }
    }
}
