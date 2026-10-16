using System.Collections.Generic;
using UnityEngine;

// Player 이외에도 Boss/Altar 등 이름 있는 위치를 전역 객체 검색 없이 재사용할 수 있습니다.
[DisallowMultipleComponent]
public sealed class EffectPositionAnchor : MonoBehaviour
{
    public string anchorKey = "Player";
    [Tooltip("선택 사항. 특정 소유자의 위치라면 연결하고, 비우면 공용 위치로 등록합니다.")]
    public GameObject owner;
    private static readonly List<EffectPositionAnchor> anchors = new List<EffectPositionAnchor>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAnchors() => anchors.Clear();
    private void OnEnable() { if (!anchors.Contains(this)) anchors.Add(this); }
    private void OnDisable() => anchors.Remove(this);
    public static bool TryGetPosition(string key, GameObject owner, out Vector3 position)
    {
        EffectPositionAnchor fallback = null;
        for (int i = anchors.Count - 1; i >= 0; i--)
        {
            var anchor = anchors[i];
            if (anchor == null || !anchor.isActiveAndEnabled || anchor.anchorKey != key) continue;
            if (owner != null && anchor.owner == owner) { position = anchor.transform.position; return true; }
            if (anchor.owner == null && fallback == null) fallback = anchor;
        }
        position = fallback != null ? fallback.transform.position : Vector3.zero;
        return fallback != null;
    }
}
