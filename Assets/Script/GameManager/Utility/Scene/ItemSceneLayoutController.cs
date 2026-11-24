using UnityEngine;

// 아이템씬 단독 실행과 General 내 40% 표시에서 같은 UI 내부 비율을 사용합니다.
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class ItemSceneLayoutController : MonoBehaviour
{
    [Header("기존 UI 영역 연결")]
    public RectTransform gameArea;
    public RectTransform backgroundGameArea;
    public RectTransform seriesArea;
    public RectTransform materialsArea;
    public RectTransform recipeArea;
    [Header("아이템 화면 내부 비율")]
    [Tooltip("실제 게임 영역의 X/Y 비율입니다. 기본값은 가로80%·세로80%, 나머지는 외곽 UI입니다.")]
    public Vector2 gameAreaShare = new Vector2(.8f, .8f);
    [Range(.05f, .95f)] public float lowerSidebarShare = .5f;
    [Header("기존 월드 박스 연결")]
    public UIWorldBoundsFitter worldBox;

    private void OnEnable()
    {
        Canvas.preWillRenderCanvases += ApplyRegions;
        Canvas.willRenderCanvases += FitWorld;
        ApplyRegions();
    }
    private void OnDisable()
    {
        Canvas.preWillRenderCanvases -= ApplyRegions;
        Canvas.willRenderCanvases -= FitWorld;
    }
    private void LateUpdate() => ApplyRegions();
    public void ApplyRegions()
    {
        SceneUILayout.ItemRegions(gameAreaShare, lowerSidebarShare, out var game, out var series, out var materials, out var recipe);
        SceneUILayout.SetRect(gameArea, game); SceneUILayout.SetRect(backgroundGameArea, game);
        SceneUILayout.SetRect(seriesArea, series); SceneUILayout.SetRect(materialsArea, materials);
        SceneUILayout.SetRect(recipeArea, recipe);
    }
    // Canvas Scaler/레이아웃 확정 후 프레임을 월드 평면으로 투영합니다.
    private void FitWorld() { if (worldBox != null && !worldBox.autoFit) worldBox.FitNow(); }
}
