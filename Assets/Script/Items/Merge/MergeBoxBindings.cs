using UnityEngine;

// 상자 프리팹 내부 참조입니다. 씬의 매니저가 이름 검색 없이 투입구를 연결합니다.
public sealed class MergeBoxBindings : MonoBehaviour
{
    public Transform spawnPoint;
    public MergeDropZone dropZone;
}
