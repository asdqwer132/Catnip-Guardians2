using UnityEngine;

[CreateAssetMenu(fileName = "MergeItem", menuName = "04.Items/Merge/MergeItem")]
public class MergeItem : ScriptableObject
{
    public ItemData item;
    [Tooltip("이 머지 아이템을 생성할 프리팹입니다. 루트에 MergeItemObject가 있어야 합니다. 아이템별 Prefab Variant를 연결하면 비주얼만 따로 수정할 수 있습니다.")]
    public GameObject prefab;
    [Min(0.01f)] public float radius = 0.5f;
    public float Radius => EffectStatUtility.Safe(radius, 0.01f, 1000f, 0.5f);
}
