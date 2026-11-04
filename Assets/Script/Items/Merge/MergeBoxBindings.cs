using UnityEngine;

// 상자 프리팹 내부 참조입니다. 씬의 매니저가 이름 검색 없이 투입구를 연결합니다.
public sealed class MergeBoxBindings : MonoBehaviour
{
    public MergeBoxSpritePart[] visuals = System.Array.Empty<MergeBoxSpritePart>();
    [SerializeField, HideInInspector] private Vector2 referenceSize;
    [SerializeField, HideInInspector] private Vector3 spawnPosition, linePosition;
    [SerializeField, HideInInspector] private Vector2 lineSize;
    [SerializeField, HideInInspector] private Rect originalMovement;
    public bool HasVisualLayout => referenceSize.x > 0 && referenceSize.y > 0;

    // Explicit capture keeps repeated edits relative to authored geometry, without cumulative scaling.
    public void CaptureVisualLayout(Vector2 size)
    {
        referenceSize = Vector2.Max(Vector2.one * .01f, size);
        foreach (var part in visuals) part?.Capture(transform);
        if (spawnPoint != null) spawnPosition = transform.InverseTransformPoint(spawnPoint.position);
        if (dropZone == null) return;
        originalMovement = dropZone.movementArea;
        if (dropZone.lineSprite == null) return;
        linePosition = transform.InverseTransformPoint(dropZone.lineSprite.transform.position);
        lineSize = dropZone.lineSprite.size;
    }
    public void ApplyVisualLayout(Vector2 size, Vector2 spawnOffset, Vector2 deadlineOffset)
    {
        if (!HasVisualLayout) return;
        Vector2 ratio = size / referenceSize;
        foreach (var part in visuals) part?.Apply(transform, size - referenceSize, ratio);
        MergeBoxSpritePart.Place(transform, spawnPoint, spawnPosition, ratio);
        if (spawnPoint != null) spawnPoint.position += transform.TransformVector(spawnOffset);
        if (dropZone == null) return;
        dropZone.movementArea = Rect.MinMaxRect(originalMovement.xMin * ratio.x, originalMovement.yMin * ratio.y,
            originalMovement.xMax * ratio.x, originalMovement.yMax * ratio.y);
        if (dropZone.lineSprite != null)
        {
            MergeBoxSpritePart.Place(transform, dropZone.lineSprite.transform, linePosition, ratio);
            dropZone.lineSprite.transform.position += transform.TransformVector(deadlineOffset);
            dropZone.lineSprite.size = new Vector2(lineSize.x * ratio.x, lineSize.y);
        }
        dropZone.RefreshLine();
    }

    public Transform spawnPoint;
    public MergeDropZone dropZone;
}
