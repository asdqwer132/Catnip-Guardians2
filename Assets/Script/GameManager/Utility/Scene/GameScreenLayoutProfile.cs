using UnityEngine;

// General의 UI 배치를 저장합니다. 씬 오브젝트 참조는 저장하지 않습니다.
[CreateAssetMenu(menuName = "Game/Screen Layout Profile")]
public sealed class GameScreenLayoutProfile : ScriptableObject
{
    public SceneUIScaleProfile uiScale;
    [Tooltip("Fill Region은 영역을 채웁니다. Integer Pixels는 공통 정수 배율을 사용하며 경계에 몇 픽셀의 여백이 생길 수 있습니다.")]
    public WorldPixelScaling pixelScaling;
    [Min(1)] public int worldPixelsPerUnit = 32;
    [Tooltip("전체 화면 높이에 대응하는 월드 영상 픽셀 수. UI 기준 해상도와 별개인 렌더링 해상도입니다.")]
    [Min(1)] public int worldReferenceHeight = 320;
    [ReadOnly] public Rect navigation = new Rect(0,.85f,1,.15f);
    [ReadOnly] public Rect battle = new Rect(0,0,.6f,.85f);
    [ReadOnly] public Rect item = new Rect(.6f,0,.4f,.85f);
    [SerializeField, HideInInspector] private SavedScreenRegion savedNavigation, savedBattle, savedItem;
    public void Capture(GameScreenRegion region, RectTransform area, Rect viewport, Vector2 canvasSize)
    {
        var saved = SavedScreenRegion.Capture(area, viewport, canvasSize);
        if(region == GameScreenRegion.Navigation) savedNavigation=saved;
        else if(region == GameScreenRegion.Battle) savedBattle=saved;
        else savedItem=saved;
    }
    public Rect Region(GameScreenRegion region,Vector2 output)
    {
        var saved=region==GameScreenRegion.Navigation?savedNavigation:region==GameScreenRegion.Battle?savedBattle:savedItem;
        float scale=uiScale!=null?uiScale.CalculateScale(output):output.y/1440f;
        return saved!=null?saved.Evaluate(output/Mathf.Max(.0001f,scale)):Region(region);
    }
    public Rect Region(GameScreenRegion region) => region == GameScreenRegion.Battle ? battle : region == GameScreenRegion.Item ? item : navigation;
    public float WorldScale(Vector2 output)
    {
        float ui = uiScale != null ? uiScale.CalculateScale(output) : output.y / 1440f;
        float referenceHeight = uiScale != null ? uiScale.referenceResolution.y : 1440f;
        float scale = Mathf.Max(.0001f, ui * referenceHeight / Mathf.Max(1,worldReferenceHeight));
        return pixelScaling == WorldPixelScaling.IntegerPixels ? Mathf.Max(1,Mathf.Floor(scale)) : scale;
    }
    public Vector2Int RenderSize(Rect viewport, Vector2 output)
    {
        float scale = WorldScale(output);
        return new Vector2Int(Mathf.Clamp(RoundPixels(viewport.width * output.x / scale),1,8192),
            Mathf.Clamp(RoundPixels(viewport.height * output.y / scale),1,8192));
    }
    private int RoundPixels(float pixels)
    {
        // Pixel Perfect Camera requires even render dimensions to avoid half-pixel centers.
        int pairs=pixelScaling==WorldPixelScaling.IntegerPixels?Mathf.FloorToInt(pixels*.5f):Mathf.RoundToInt(pixels*.5f);
        return Mathf.Max(2,pairs*2);
    }
    public Rect DisplayRegion(Rect allocation,Vector2 output,Vector2Int renderSize)
    {
        if(pixelScaling != WorldPixelScaling.IntegerPixels) return allocation;
        Vector2 size=(Vector2)renderSize*WorldScale(output);
        Vector2 min=Vector2.Scale(allocation.center,output)-size*.5f;
        min=new Vector2(Mathf.Round(min.x),Mathf.Round(min.y));
        return new Rect(min/output,size/output);
    }
}
public enum WorldPixelScaling { FillRegion, IntegerPixels }
public enum GameScreenRegion { Navigation, Battle, Item }
