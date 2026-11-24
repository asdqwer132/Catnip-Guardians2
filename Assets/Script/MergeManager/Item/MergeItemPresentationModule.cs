using UnityEngine;

// 파티클, 애니메이션 등 연출별로 상속해 확장합니다. 반환 시 반복 연출을 정리합니다.
public abstract class MergeItemPresentationModule : MonoBehaviour
{
    public abstract void Play(MergeItemPresentation source, MergeItemPresentationEvent trigger, string gimmickId);
    public abstract void ResetPresentation();
}
