using UnityEngine;

// 씬에서 공유 상자의 벽, 배경과 투입구를 연결합니다.
public sealed class MergeBoxBindings : MonoBehaviour
{
    [Tooltip("벽, 배경, LeftDeco, RightDeco를 역할별로 연결합니다.")]
    public MergeBoxSpritePart[] visuals = System.Array.Empty<MergeBoxSpritePart>();
    public Transform spawnPoint;
    public MergeDropZone dropZone;
}
