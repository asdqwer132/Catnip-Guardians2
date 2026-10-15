using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public sealed class MergeGameSceneController : MonoBehaviour
{
    public MergeGameCatalog catalog;
    public Camera combatCamera;
    public Player player;
    public ItemUsePositionProvider positionProvider;
    public ItemThrowExecutor throwExecutor;
    public Behaviour[] legacyInputComponents;
    public GameObject[] legacyGameplayUI;
    public Canvas combatHud;
    public Canvas resultCanvas;
    [Range(8, 31)] public int containerLayer = 31;
    public MergeGameCatalog Catalog => catalog;
    public InventoryManager Inventory { get; private set; }
    public int ContainerLayer => containerLayer;
    public Sprite CircleSprite { get; private set; }
    public Sprite SquareSprite { get; private set; }
    public Material SpriteMaterial { get; private set; }
    public bool HasTarget { get; private set; }
    public Vector3 TargetPosition { get; private set; }
    public MergeContainerRuntime Current => current >= 0 && current < containers.Count ? containers[current] : null;
    private readonly List<MergeContainerRuntime> containers = new List<MergeContainerRuntime>();
    private readonly Queue<MergeContainerTokenView> pool = new Queue<MergeContainerTokenView>();
    private readonly List<MergeContainerTokenView> deferredReturns = new List<MergeContainerTokenView>();
    private bool simulating;
    private Scene physicsScene;
    private PhysicsScene2D physics;
    private Camera containerCamera;
    private RawImage viewport;
    private TextMeshProUGUI status;
    private LineRenderer targetMarker;
    private LineRenderer selectionBrush;
    private bool selectionUsesDrag;
    private Transform physicsRoot;
    private RenderTexture texture;
    private Texture2D circleTexture, squareTexture;
    private GameInputManager subscribedInput;
    private int current;
    private bool initialized;
    private Rect previousCameraRect;
    private int previousCullingMask;

    private void Awake()
    {
        if (catalog == null || combatCamera == null) { enabled = false; return; }
        MergeGameSession.Ensure();
        Inventory = InventoryManager.instance;
        if (Inventory == null) Inventory = new GameObject("Combat Material Inventory").AddComponent<InventoryManager>();
        MergeGameSession.Instance.RestoreInventory(Inventory);
        Inventory.onInventoryChanged += SaveInventory;
        foreach (var component in legacyInputComponents ?? new Behaviour[0]) if (component != null) component.enabled = false;
        foreach (var go in legacyGameplayUI ?? new GameObject[0]) if (go != null) go.SetActive(false);
        previousCameraRect = combatCamera.rect; previousCullingMask = combatCamera.cullingMask;
        combatCamera.rect = new Rect(0f, 0f, 0.5f, 1f); combatCamera.cullingMask &= ~(1 << containerLayer);
        if (resultCanvas != null) resultCanvas.sortingOrder = Mathf.Max(resultCanvas.sortingOrder, 500);
        if (combatHud != null)
        {
            var children = new List<Transform>(); foreach (Transform child in combatHud.transform) children.Add(child);
            var left = MergeGameUI.Rect("Combat HUD Viewport", combatHud.transform, Vector2.zero, new Vector2(0.5f, 1f));
            foreach (var child in children) child.SetParent(left, false);
        }
        BuildStage(); BuildUI();
    }
    private void Start() => BindInput();
    private void BindInput()
    {
        if (subscribedInput == GameInputManager.instance || GameInputManager.instance == null) return;
        UnbindInput(); subscribedInput = GameInputManager.instance;
        subscribedInput.restrictPointerToGameplayCamera = true;
        subscribedInput.OnUseItemPressed += PickCombatTarget;
        subscribedInput.OnNumberPressed += SelectContainer;
    }
    private void UnbindInput()
    {
        if (subscribedInput == null) return;
        subscribedInput.OnUseItemPressed -= PickCombatTarget; subscribedInput.OnNumberPressed -= SelectContainer;
        subscribedInput = null;
    }
    public void InitializeRound()
    {
        foreach (var container in containers) { container.Clear(); container.RefreshStats(); }
        HasTarget = false; if (targetMarker != null) targetMarker.enabled = false;
        initialized = true; SelectContainer(0);
    }
    private void BuildStage()
    {
        physicsScene = SceneManager.CreateScene("Item Merge Physics " + GetInstanceID(), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        physics = physicsScene.GetPhysicsScene2D();
        physicsRoot = new GameObject("Merge Physics Root").transform;
        SceneManager.MoveGameObjectToScene(physicsRoot.gameObject, physicsScene);
        circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var colors = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) colors[y * 64 + x] = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) < 31f ? Color.white : Color.clear;
        circleTexture.SetPixels(colors); circleTexture.Apply();
        CircleSprite = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        squareTexture = new Texture2D(1, 1); squareTexture.SetPixel(0, 0, Color.white); squareTexture.Apply();
        SquareSprite = Sprite.Create(squareTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        SpriteMaterial = new Material(shader);
        foreach (var definition in catalog.containers ?? new MergeContainerDefinition[0])
        {
            if (definition == null) continue;
            var root = new GameObject(definition.displayName).transform; root.SetParent(physicsRoot, false);
            root.position = new Vector3(10000f + containers.Count * 100f, 0f, 0f);
            containers.Add(new MergeContainerRuntime(this, definition, root));
        }
        var cameraObject = new GameObject("Merge Container Camera"); cameraObject.transform.SetParent(physicsRoot, false);
        containerCamera = cameraObject.AddComponent<Camera>(); containerCamera.CopyFrom(combatCamera);
        containerCamera.enabled = false;
        containerCamera.rect = new Rect(0, 0, 1, 1); containerCamera.cullingMask = 1 << containerLayer;
        containerCamera.clearFlags = CameraClearFlags.SolidColor; containerCamera.backgroundColor = new Color(0.035f, 0.055f, 0.08f);
        containerCamera.orthographic = true;
        var marker = new GameObject("Item Throw Target"); marker.transform.SetParent(transform, false);
        targetMarker = marker.AddComponent<LineRenderer>(); targetMarker.sharedMaterial = SpriteMaterial;
        targetMarker.startColor = targetMarker.endColor = new Color(1f, 0.8f, 0.2f);
        targetMarker.startWidth = targetMarker.endWidth = 0.04f; targetMarker.loop = true;
        targetMarker.positionCount = 40; targetMarker.sortingOrder = 50; targetMarker.enabled = false;
        var brush = new GameObject("Drag Selection Radius"); brush.layer = containerLayer; brush.transform.SetParent(physicsRoot, false);
        selectionBrush = brush.AddComponent<LineRenderer>(); selectionBrush.sharedMaterial = SpriteMaterial;
        selectionBrush.startColor = selectionBrush.endColor = new Color(1f, 0.8f, 0.2f);
        selectionBrush.startWidth = selectionBrush.endWidth = 0.035f; selectionBrush.loop = true;
        selectionBrush.positionCount = 40; selectionBrush.sortingOrder = 20; selectionBrush.enabled = false;
    }
    private void BuildUI()
    {
        var ui = new GameObject("Merge Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ui.transform.SetParent(transform, false); var canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scaler = ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var right = MergeGameUI.Panel("Merge Panel", ui.transform, new Vector2(0.5f, 0), Vector2.one, new Color(0.035f, 0.055f, 0.08f));
        var title = MergeGameUI.Rect("Title", right.transform, new Vector2(0.02f, 0.94f), new Vector2(0.98f, 1f));
        MergeGameUI.Label(title, "아이템 합성 통 · 왼쪽 화면을 클릭해 투척 위치 지정", catalog.UIFont, 20);
        var tabs = MergeGameUI.Rect("Series", right.transform, new Vector2(0.02f, 0.875f), new Vector2(0.98f, 0.94f));
        var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 4f;
        for (int i = 0; i < containers.Count; i++)
        {
            int index = i; MergeGameUI.Button(tabs, $"{i + 1}. {containers[i].Definition.displayName}", catalog.UIFont, () => SelectContainer(index));
        }
        var image = MergeGameUI.Rect("Container View", right.transform, new Vector2(0.025f, 0.16f), new Vector2(0.975f, 0.87f));
        viewport = image.gameObject.AddComponent<RawImage>();
        var pointer = image.gameObject.AddComponent<MergeContainerPointerInput>(); pointer.controller = this;
        var text = MergeGameUI.Rect("Status", right.transform, new Vector2(0.025f, 0.075f), new Vector2(0.975f, 0.16f));
        status = MergeGameUI.Label(text, "", catalog.UIFont, 18);
        var buttons = MergeGameUI.Rect("Selection", right.transform, new Vector2(0.025f, 0.01f), new Vector2(0.975f, 0.07f));
        var buttonLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>(); buttonLayout.spacing = 8f;
        MergeGameUI.Button(buttons, "선택 취소", catalog.UIFont, ClearSelection);
        if (catalog.selectionDelivery == MergeSelectionDelivery.ConfirmSelection)
            MergeGameUI.Button(buttons, "선택 아이템 던지기", catalog.UIFont, () => ThrowSelected(selectionUsesDrag));
    }
    private void SaveInventory() => MergeGameSession.Ensure().SaveInventory(Inventory);
    private void Update()
    {
        BindInput(); if (viewport == null || containerCamera == null) return;
        UpdateTextureAndCamera();
        if (initialized && Time.deltaTime > 0f)
            foreach (var container in containers) container.Tick(Time.deltaTime, catalog.injectIntoInactiveContainers || container == Current);
        if (Current != null)
        {
            int stock = 0;
            foreach (var item in Current.Definition.injectionMaterials ?? new ItemData[0]) if (item != null && Inventory != null) stock += Inventory.GetItemAmount(item);
            status.text = $"{Current.Definition.displayName} · 통 안 {Current.Board.Count}개 · 재료 {stock}개 · 선택 {Current.Board.SelectedIds().Count}개\n" +
                (HasTarget ? $"클릭 {Current.Board.Cooldown(false):0.0}초 / 드래그 {Current.Board.Cooldown(true):0.0}초" : "먼저 왼쪽 전투 화면에서 투척 위치를 선택하세요") +
                (Current.InletBlocked ? " · 주입구가 막혔어요" : "");
        }
    }
    private void FixedUpdate()
    {
        if (!initialized || !physics.IsValid() || Time.timeScale <= 0f) return;
        simulating = true;
        try { physics.Simulate(Time.fixedDeltaTime); }
        finally
        {
            simulating = false;
            // 같은 물리 스텝의 이전 충돌 콜백이 재사용된 새 아이템에 전달되지 않게 반환을 미룹니다.
            foreach (var view in deferredReturns) if (view != null) pool.Enqueue(view);
            deferredReturns.Clear();
        }
    }
    private void UpdateTextureAndCamera()
    {
        var corners = new Vector3[4]; viewport.rectTransform.GetWorldCorners(corners);
        int width = Mathf.Clamp(Mathf.RoundToInt(corners[2].x - corners[0].x), 64, 2048);
        int height = Mathf.Clamp(Mathf.RoundToInt(corners[2].y - corners[0].y), 64, 2048);
        if (texture == null || texture.width != width || texture.height != height)
        {
            containerCamera.targetTexture = null; if (texture != null) { texture.Release(); Destroy(texture); }
            texture = new RenderTexture(width, height, 16); texture.Create(); viewport.texture = texture; containerCamera.targetTexture = texture;
        }
        containerCamera.enabled = true;
        if (Current == null) return;
        containerCamera.aspect = (float)width / height;
        containerCamera.transform.position = Current.Root.position + new Vector3(0f, Current.Stats.height * 0.5f, -10f);
        containerCamera.transform.rotation = Quaternion.identity;
        containerCamera.orthographicSize = Mathf.Max(Current.Stats.height * 0.5f + 0.8f, (Current.Stats.width * 0.5f + 0.8f) / containerCamera.aspect);
    }
    public void SelectContainer(int index)
    {
        if (index < 0 || index >= containers.Count) return;
        ClearSelection(); current = index;
    }
    public void ClearSelection()
    {
        selectionUsesDrag = false;
        if (selectionBrush != null) selectionBrush.enabled = false;
        if (Current != null) { Current.Board.ClearSelection(); Current.RefreshSelection(); }
    }
    public void MarkDragSelection(Vector3 point, bool visible)
    {
        if (selectionBrush == null || Current == null) return;
        selectionBrush.enabled = visible; if (!visible) return;
        selectionUsesDrag = true;
        for (int i = 0; i < 40; i++) selectionBrush.SetPosition(i, point + new Vector3(Mathf.Cos(i * Mathf.PI / 20f), Mathf.Sin(i * Mathf.PI / 20f)) * Current.Stats.dragRadius);
    }
    private void PickCombatTarget()
    {
        if (!CanInteract() || subscribedInput == null || !combatCamera.pixelRect.Contains(subscribedInput.MouseScreenPosition)) return;
        Vector3 position = subscribedInput.MouseWorldPosition;
        var provider = positionProvider != null ? positionProvider : player != null ? player.GetComponent<ItemUsePositionProvider>() : null;
        if (provider != null) position = provider.ClampPositionByPlayerRange(position);
        TargetPosition = position; HasTarget = true; targetMarker.enabled = true;
        float radius = Mathf.Max(0.2f, catalog.scatterRadius);
        for (int i = 0; i < 40; i++) targetMarker.SetPosition(i, position + new Vector3(Mathf.Cos(i * Mathf.PI / 20f), Mathf.Sin(i * Mathf.PI / 20f), 0f) * radius);
    }
    public bool CanInteract() => initialized && Time.timeScale > 0f &&
        (GameInputManager.instance == null || !GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay));
    public Vector3 PointerWorld(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport.rectTransform, screen, null, out var local);
        Rect rect = viewport.rectTransform.rect;
        return containerCamera.ViewportToWorldPoint(new Vector3((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height, 10f));
    }
    public bool PointerInside(Vector2 screen) => viewport != null && RectTransformUtility.RectangleContainsScreenPoint(viewport.rectTransform, screen, null);
    public void ThrowSelected(bool drag)
    {
        var container = Current;
        if (!CanInteract() || !HasTarget || container == null || !container.Board.CanSelect(drag) || throwExecutor == null || player == null) return;
        int succeeded = 0;
        foreach (int id in container.Board.SelectedIds())
        {
            var token = container.Board.Get(id);
            if (token == null || !container.Views.TryGetValue(id, out var view)) continue;
            Vector3 start = player.transform.position;
            if (!catalog.throwFromOwner)
            {
                Vector3 uv = containerCamera.WorldToViewportPoint(view.transform.position);
                var corners = new Vector3[4]; viewport.rectTransform.GetWorldCorners(corners);
                Vector3 screen = new Vector3(Mathf.Lerp(corners[0].x, corners[2].x, uv.x), Mathf.Lerp(corners[0].y, corners[2].y, uv.y), -combatCamera.transform.position.z);
                start = combatCamera.ScreenToWorldPoint(screen);
            }
            start += catalog.throwStartOffset; start.z = 0f;
            Vector3 target = TargetPosition + (Vector3)(Random.insideUnitCircle * Mathf.Max(0f, catalog.scatterRadius)); target.z = 0f;
            bool thrown = throwExecutor.Throw(token.item, start, target, player.gameObject, null, 0, options: container.Definition.throwOptions);
            if (container.Board.CommitThrow(id, thrown)) { container.ReturnView(id); succeeded++; }
        }
        if (succeeded > 0) container.Board.SpendCooldown(drag, drag ? container.Stats.dragCooldown : container.Stats.clickCooldown);
        ClearSelection();
    }
    public MergeContainerTokenView TakeView()
    {
        MergeContainerTokenView view = null;
        while (pool.Count > 0 && view == null) view = pool.Dequeue();
        if (view == null)
        {
            var go = new GameObject("Merge Item"); go.layer = containerLayer; go.transform.SetParent(physicsRoot, false);
            view = go.AddComponent<MergeContainerTokenView>();
        }
        view.gameObject.SetActive(true); return view;
    }
    public void ReturnView(MergeContainerTokenView view)
    {
        if (view == null) return;
        view.Release(); view.transform.SetParent(physicsRoot, false);
        if (simulating) deferredReturns.Add(view); else pool.Enqueue(view);
    }
    private void OnDisable() { UnbindInput(); ClearSelection(); if (containerCamera != null) containerCamera.enabled = false; }
    private void OnDestroy()
    {
        if (Inventory != null) { SaveInventory(); Inventory.onInventoryChanged -= SaveInventory; }
        if (combatCamera != null) { combatCamera.rect = previousCameraRect; combatCamera.cullingMask = previousCullingMask; }
        if (containerCamera != null) containerCamera.targetTexture = null;
        if (texture != null) { texture.Release(); Destroy(texture); }
        if (physicsScene.IsValid() && physicsScene.isLoaded) SceneManager.UnloadSceneAsync(physicsScene);
        if (CircleSprite != null) Destroy(CircleSprite); if (SquareSprite != null) Destroy(SquareSprite);
        if (circleTexture != null) Destroy(circleTexture); if (squareTexture != null) Destroy(squareTexture);
        if (SpriteMaterial != null) Destroy(SpriteMaterial);
    }
}
