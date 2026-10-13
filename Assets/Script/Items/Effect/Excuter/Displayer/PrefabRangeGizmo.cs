using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Catnip/Debug/Prefab Range Gizmo")]
public sealed class PrefabRangeGizmo : MonoBehaviour
{
    [Tooltip("비우면 자신과 부모에서 IPrefabRangeProvider를 자동으로 찾습니다.")]
    public MonoBehaviour rangeSource;
    public bool selectedOnly = true;
    public bool showLabels = true;
    public bool useCustomColor;
    public Color color = new Color(0.2f, 1f, 0.5f, 1f);
    [Header("범위 제공자가 없는 프리팹")]
    [Tooltip("Circle/Box Collider2D를 먼저 표시합니다. 콜라이더도 없으면 Preview Radius를 표시합니다.")]
    public bool useColliderFallback = true;
    public bool showManualPreview;
    [Min(0f)] public float previewRadius = 1f;
    public Vector2 previewOffset;

    public void CollectRanges(List<PrefabVisualRange> results)
    {
        if (results == null) return;
        results.Clear();
        if (rangeSource is IPrefabRangeProvider configured)
        { configured.CollectPrefabRanges(results); return; }
        if (rangeSource == null)
            foreach (MonoBehaviour parent in GetComponentsInParent<MonoBehaviour>(true))
                if (parent is IPrefabRangeProvider source)
                { source.CollectPrefabRanges(results); return; }
        if (useColliderFallback)
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
            {
                PrefabVisualRange range;
                if (PrefabVisualRange.TryCollider(collider, collider.GetType().Name, out range)) results.Add(range);
            }
        if (results.Count == 0 && showManualPreview)
            results.Add(PrefabVisualRange.Circle(transform.TransformPoint(previewOffset), previewRadius,
                "Preview (수동)", PrefabRangeKind.Preview));
    }
}
