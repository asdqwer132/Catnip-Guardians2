using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public enum NavigationSizeMode { ScreenRatio, MeasuredPanel }

// 새 UI를 생성하지 않고 기존 씬 카메라와 Canvas/패널의 표시 영역을 연결합니다.
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
    [Range(0.1f, 0.9f)] public float battleRatio = 0.6f;
    public bool battleFirst = true;
    [Range(0f, 0.1f)] public float gap;
    [Tooltip("기존 Screen Space Overlay Canvas를 해당 씬의 카메라에 연결합니다. 코어 UI는 전체 화면으로 유지합니다.")]
    public bool fitSceneCanvases = true;
    [Header("직접 만든 상단 내비게이션 UI")]
    [Tooltip("제너럴 씬의 루트 Canvas. 같은 씬의 Navigation 카메라와 연결합니다.")]
    public Canvas navigationCanvas;
    [Tooltip("기존 상단 바 Panel입니다. Screen Ratio에서는 화면 비율에 맞춰 크기를 정하고, Measured Panel에서는 이 패널의 하단을 측정합니다.")]
    public RectTransform navigationArea;
    public bool reserveNavigationSpace = true;
    [Tooltip("Screen Ratio는 UI와 카메라 경계를 같은 화면 비율로 맞춥니다. Measured Panel은 직접 정한 패널 높이를 측정합니다.")]
    public NavigationSizeMode navigationSizeMode = NavigationSizeMode.ScreenRatio;
    [Range(0f, 0.8f)] public float navigationHeightRatio = 0.15f;
    [Tooltip("같은 Sorting Layer 안의 Canvas 순서. 기본 내비게이션 100 / 코어 설정 200 / 로딩 300입니다.")]
    public int navigationSortingOrder = 100;
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
    private SceneCameraBinding navigationView;
    private int appliedNavigationLayer = -1;
    public Camera NavigationCamera => navigationView != null ? navigationView.Camera : null;
    public bool IsReady => ready;
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
            PrepareContentScene(battle);
            item = SceneManager.GetSceneByName(itemScene);
            if (!item.isLoaded)
                yield return SceneManager.LoadSceneAsync(itemScene, new LoadSceneParameters(LoadSceneMode.Additive,
                    isolateItemPhysics ? LocalPhysicsMode.Physics2D : LocalPhysicsMode.None));
            item = SceneManager.GetSceneByName(itemScene);
            PrepareContentScene(item);
            if (!battle.isLoaded || !item.isLoaded) yield break;
            itemPhysics = item.GetPhysicsScene2D();
            if (BattleCamera == null || ItemCamera == null)
            { Debug.LogError("배틀/아이템 카메라에 SceneCameraBinding을 붙이고 Role을 Content로 설정하세요.", this); yield break; }
            Canvas.ForceUpdateCanvases();
            ready = true; ApplyLayout();
        }
        finally { loading = false; }
    }
    private void OnContentSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (loading || ready) PrepareContentScene(scene);
    }
    private void PrepareContentScene(Scene scene)
    {
        if (!scene.isLoaded) return;
        bool isItem = scene.name == itemScene;
        if (!isItem && scene.name != battleScene) return;
        var view = SceneCameraBinding.ContentIn(scene);
        if (view == null || (isItem ? itemView == view : battleView == view)) return;
        view.SetComposed(true);
        if (isItem)
        {
            item = scene; itemView = view; ItemCamera = view.Camera;
            foreach (var root in scene.GetRootGameObjects()) SetItemLayer(root.transform);
        }
        else { battle = scene; battleView = view; BattleCamera = view.Camera; }
        if (fitSceneCanvases) ConnectCanvases(scene, view.Camera);
        // sceneLoaded은 새 씬의 첫 렌더보다 먼저 옵니다. 다른 자식 씬을 기다리지 않습니다.
        Canvas.ForceUpdateCanvases();
        ApplyLayout();
    }
    private void SetItemLayer(Transform node)
        => SetLayer(node, itemLayer);
    private static void SetLayer(Transform node, int layer)
    {
        // 기존 계층을 해당 카메라의 표시 레이어에 연결합니다.
        node.gameObject.layer = layer;
        foreach (Transform child in node) SetLayer(child, layer);
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
        navigationView = SceneCameraBinding.NavigationIn(gameObject.scene);
        if (navigationView != null && navigationCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            // Navigation 프로필이 단일 레이어일 때, 편집 중 UI 기본 레이어로
            // 생성한 버튼/텍스트도 같은 카메라에서 보이도록 기존 계층만 연결합니다.
            int mask = navigationView.Camera.cullingMask;
            if (mask != 0 && (mask & (mask - 1)) == 0)
                for (int layer = 0; layer < 32; layer++)
                    if (mask == (1 << layer))
                    {
                        if (appliedNavigationLayer != layer)
                        { SetLayer(navigationCanvas.transform, layer); appliedNavigationLayer = layer; }
                        break;
                    }
            if (navigationCanvas.renderMode != RenderMode.ScreenSpaceCamera) navigationCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (navigationCanvas.worldCamera != navigationView.Camera) navigationCanvas.worldCamera = navigationView.Camera;
            float distance = Mathf.Clamp(navigationCanvas.planeDistance,
                navigationView.Camera.nearClipPlane + .1f, navigationView.Camera.farClipPlane - .1f);
            if (navigationCanvas.planeDistance != distance) navigationCanvas.planeDistance = distance;
        }
        else
        {
            // 직접 설정한 Overlay는 유지합니다. 카메라가 없는 경우에도 기존 UI를 확인할 수 있습니다.
            if (navigationCanvas.renderMode != RenderMode.ScreenSpaceOverlay) navigationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (navigationCanvas.worldCamera != null) navigationCanvas.worldCamera = null;
        }
        if (navigationSizeMode == NavigationSizeMode.ScreenRatio && navigationArea != null &&
            navigationArea.parent == navigationCanvas.transform)
        {
            // Overlay는 전체 화면의 위 15%, Camera Canvas는 이미 잘린 헤더 전체를 채웁니다.
            float bottom = navigationCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? 1f - RatioHeight : 0f;
            SceneUILayout.SetRect(navigationArea, Rect.MinMaxRect(0f, bottom, 1f, 1f));
        }
        if (navigationCanvas.sortingOrder != navigationSortingOrder) navigationCanvas.sortingOrder = navigationSortingOrder;
    }
    private float RatioHeight => float.IsNaN(navigationHeightRatio) ? 0f : Mathf.Clamp(navigationHeightRatio, 0f, .8f);
    public float NavigationTopInset => reserveNavigationSpace ? NavigationHeight : 0f;
    public float NavigationHeight
    {
        get
        {
            if (navigationState == null || navigationCanvas == null || !navigationCanvas.isActiveAndEnabled)
                return 0f;
            if (navigationArea != null && !navigationArea.gameObject.activeInHierarchy) return 0f;
            if (navigationSizeMode == NavigationSizeMode.ScreenRatio) return RatioHeight;
            float ratio = navigationHeightRatio;
            if (navigationArea != null)
            {
                if (!navigationArea.gameObject.activeInHierarchy || !navigationArea.IsChildOf(navigationCanvas.transform)) return 0f;
                if (Screen.height <= 0) return 0f;
                navigationArea.GetWorldCorners(navigationCorners);
                float bottom = float.PositiveInfinity;
                foreach (var corner in navigationCorners)
                    bottom = Mathf.Min(bottom, RectTransformUtility.WorldToScreenPoint(navigationCanvas.worldCamera, corner).y);
                ratio = 1f - Mathf.Floor(bottom) / Screen.height;
            }
            return float.IsNaN(ratio) ? 0f : Mathf.Clamp(ratio, 0f, 0.8f);
        }
    }
    public void ApplyLayout()
    {
        if (!isActiveAndEnabled) return;
        ConnectNavigation();
        float top = NavigationTopInset;
        if (navigationView != null)
        {
            float navigationHeight = NavigationHeight;
            ApplyCamera(navigationView.Camera, new Rect(0, 1f - navigationHeight, 1, navigationHeight));
        }
        if (BattleCamera == null && ItemCamera == null) return;
        GameViewportLayout.Calculate(layout, battleRatio, battleFirst, gap, top, out var battleRect, out var itemRect);
        ApplyCamera(BattleCamera, battleRect); ApplyCamera(ItemCamera, itemRect);
        SceneCameraBinding.Refresh();
        SceneAudioListenerOwner.Refresh();
    }
    private static void ApplyCamera(Camera camera, Rect rect)
    {
        if (camera == null) return;
        bool visible = rect.width > 0 && rect.height > 0;
        if (camera.enabled != visible) camera.enabled = visible;
        if (visible && camera.rect != rect) camera.rect = rect;
    }
    private void LateUpdate() => ApplyLayout();
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnContentSceneLoaded;
        Canvas.willRenderCanvases += ApplyLayout;
        RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
        ApplyLayout();
    }
    private void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == BattleCamera || camera == ItemCamera || camera == NavigationCamera) ApplyLayout();
    }
    private void RestoreNavigation() { navigationState?.Restore(); navigationState = null; appliedNavigationLayer = -1; }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnContentSceneLoaded;
        Canvas.willRenderCanvases -= ApplyLayout;
        RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        RestoreNavigation();
    }
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
