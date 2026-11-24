using UnityEngine;
using UnityEngine.UI;

// 기존 루트 Canvas에 붙이는 영상 표시 컴포넌트. 새 UI나 레이아웃을 생성하지 않습니다.
[AddComponentMenu("UI/Scene Viewport Surface")]
public sealed class SceneViewportSurface : RawImage
{
    protected override void OnPopulateMesh(VertexHelper vh) { if(texture==null) vh.Clear();else base.OnPopulateMesh(vh); }
    protected override void Awake() { base.Awake(); raycastTarget = false; }
}
