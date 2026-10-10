using UnityEngine;

// 플래그는 이름 문자열이 아니라 에셋 참조로 구분한다.
[CreateAssetMenu(fileName = "BuffClassificationTag", menuName = "04.Items/Effects/Direct Effects/Buff/BuffSetting/Classification/Tag")]
public sealed class BuffFlagDefinition : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
}
