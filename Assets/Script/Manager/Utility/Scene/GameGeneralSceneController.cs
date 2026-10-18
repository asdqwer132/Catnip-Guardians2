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
    [Tooltip("제너럴 통합 실행에 필요한 설정/재화/세션을 코어에서 불러옵니다. 배틀/아이템 단독 실행에는 영향이 없습니다.")]
    public bool loadCoreScene = true;
    [Header("화면 분할")]
    public GameScreenLayout layout;
    [Range(0.1f, 0.9f)] public float battleRatio = 0.5f;
    public bool battleFirst = true;
    [Range(0f, 0.1f)] public float gap;
    [Tooltip("기존 Screen Space Overlay Canvas를 해당 씬의 카메라에 연결합니다. 코어 UI는 전체 화면으로 유지합니다.")]
    public bool fitSceneCanvases = true;
    [Header("직접 만든 상단 내비게이션 UI")]
    [Tooltip("제너럴 씬의 루트 Canvas를 연결하세요. Overlay로 표시하므로 별도의 카메라/Light/AudioListener가 필요 없습니다.")]
    public Canvas navigationCanvas;
    [Tooltip("선택 사항: 상단 바 Panel. 연결하면 실제 화면상의 하단 경계까지 공간을 확보합니다. 비우면 Height Ratio를 사용합니다.")]
    public RectTransform navigationArea;
    public bool reserveNavigationSpace = true;
    [Range(0f, 0.8f)] public float navigationHeightRatio = 0.12f;
    [Tooltip("Overlay Canvas 표시 순서. 기본 5는 코어 설정 Canvas(10)보다 아래입니다.")]
    public int navigationSortingOrder = 5;
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
    private SceneCameraBinding battleView, itemView;
    private PhysicsScene2D itemPhysics;
    private readonly List<CanvasState> canvases = new List<CanvasState>();
    private CanvasState navigationState;
    private readonly Vector3[] navigationCorners = new Vector3[4];
    private sealed class CanvasState
    {
        public Canvas canvas; public RenderMode mode; public Camera camera; public float distance;
        public int sortingLayer, sortingOrder;
        public static CanvasState Capture(Canvas value) => new CanvasState
        {
            canvas = value, mode = value.renderMode, camera = value.worldCamera, distance = value.planeDistance,
            sortingLayer = value.sortingLayerID, sortingOrder = value.sortingOrder
        };
        public void Restore()
        {
            if (canvas == null) return;
            canvas.renderMode = mode; canvas.worldCamera = camera; canvas.planeDistance = distance;
            canvas.sortingLayerID = sortingLayer; canvas.sortingOrder = sortingOrder;
        }
    }
    private IEnumerator Start()
    {
        yield return EnsureCore();
        if (autoLoadChildren && (SceneMoveManager.instance == null || !SceneMoveManager.instance.IsLoading)) yield return LoadChildren();
    }
    private IEnumerator EnsureCore()
    {
        if (loadCoreScene && !SceneManager.GetSceneByName(GameSceneRoutes.Core).isLoaded)
            yield return SceneManager.LoadSceneAsync(GameSceneRoutes.Core, LoadSceneMode.Additive);
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
            yield return EnsureCore();
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
            battleView = SceneCameraBinding.ContentIn(battle); itemView = SceneCameraBinding.ContentIn(item);
            BattleCamera = battleView != null ? battleView.Camera : null;
            ItemCamera = itemView != null ? itemView.Camera : null;
            if (BattleCamera == null || ItemCamera == null)
            { Debug.LogError("배틀/아이템 카메라에 SceneCameraBinding을 붙이고 Role을 Content로 설정하세요.", this); yield break; }
            // 전투 레이어는 보존하고, 서브 게임의 월드 오브젝트만 전용 레이어로 분리합니다.
            foreach (var root in item.GetRootGameObjects()) SetItemLayer(root.transform);
            battleView.SetComposed(true); itemView.SetComposed(true);
            if (fitSceneCanvases) { ConnectCanvases(battle, BattleCamera); ConnectCanvases(item, ItemCamera); }
            ready = true; ApplyLayout();
        }
        finally { loading = false; }
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
                canvases.Add(CanvasState.Capture(canvas));
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Clamp(canvas.planeDistance, camera.nearClipPlane + 0.1f, camera.farClipPlane - 0.1f);
            }
    }
    public void SetBattleRatio(float value) { battleRatio = value; ApplyLayout(); }
    public void SetLayout(int value) { if (value >= 0 && value <= 3) layout = (GameScreenLayout)value; ApplyLayout(); }
    public void SetNavigationHeightRatio(float value) { navigationHeightRatio = value; ApplyLayout(); }
    private void ConnectNavigation()
    {
        if (navigationState != null && navigationState.canvas != navigationCanvas) RestoreNavigation();
        // 하위 씬/코어의 Canvas를 실수로 연결해도 해당 UI를 변경하지 않습니다.
        if (navigationCanvas == null || !navigationCanvas.isRootCanvas || navigationCanvas.gameObject.scene != gameObject.scene)
        { RestoreNavigation(); return; }
        if (navigationState == null) navigationState = CanvasState.Capture(navigationCanvas);
        navigationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        navigationCanvas.worldCamera = null;
        navigationCanvas.sortingLayerID = 0;
        navigationCanvas.sortingOrder = navigationSortingOrder;
    }
    public float NavigationTopInset
    {
        get
        {
            if (!reserveNavigationSpace || navigationState == null || navigationCanvas == null || !navigationCanvas.isActiveAndEnabled)
                return 0f;
            float ratio = navigationHeightRatio;
            if (navigationArea != null)
            {
                if (!navigationArea.gameObject.activeInHierarchy || !navigationArea.IsChildOf(navigationCanvas.transform)) return 0f;
                if (Screen.height <= 0) return 0f;
                navigationArea.GetWorldCorners(navigationCorners);
                float bottom = float.PositiveInfinity;
                foreach (var corner in navigationCorners)
                    bottom = Mathf.Min(bottom, RectTransformUtility.WorldToScreenPoint(null, corner).y);
                ratio = 1f - bottom / Screen.height;
            }
            return float.IsNaN(ratio) ? 0f : Mathf.Clamp(ratio, 0f, 0.8f);
        }
    }
    public void ApplyLayout()
    {
        if (!isActiveAndEnabled) return;
        ConnectNavigation();
        if (!ready) return;
        GameViewportLayout.Calculate(layout, battleRatio, battleFirst, gap, NavigationTopInset, out var battleRect, out var itemRect);
        ApplyCamera(BattleCamera, battleRect); ApplyCamera(ItemCamera, itemRect);
        SceneCameraBinding.Refresh();
        SceneAudioListenerOwner.Refresh();
    }
    private static void ApplyCamera(Camera camera, Rect rect)
    {
        if (camera == null) return;
        camera.enabled = rect.width > 0 && rect.height > 0;
        if (camera.enabled) camera.rect = rect;
    }
    private void LateUpdate() => ApplyLayout();
    private void OnEnable() => ApplyLayout();
    private void RestoreNavigation() { navigationState?.Restore(); navigationState = null; }
    private void OnDisable() => RestoreNavigation();
    private void FixedUpdate()
    {
        if (ready && isolateItemPhysics && itemPhysics.IsValid() && itemPhysics != Physics2D.defaultPhysicsScene && Time.timeScale > 0f)
            itemPhysics.Simulate(Time.fixedDeltaTime);
    }
    private void OnDestroy()
    {
        if (battleView != null) battleView.SetComposed(false);
        if (itemView != null) itemView.SetComposed(false);
        RestoreNavigation();
        foreach (var state in canvases)
            state.Restore();
    }
}
