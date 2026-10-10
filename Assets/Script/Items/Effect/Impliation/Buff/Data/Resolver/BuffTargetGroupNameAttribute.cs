using System;
using UnityEngine;

// 그룹은 문자열로 저장해 기존 에셋과 사용자 정의 이름을 유지한다.
public sealed class BuffTargetGroupNameAttribute : PropertyAttribute
{
    public string EmptyLabel { get; }
    public BuffTargetGroupNameAttribute(string emptyLabel = "(그룹 없음)") => EmptyLabel = emptyLabel;
}

// IBuffTarget의 문자열/필드 기본값은 자동 수집한다. 복잡한 동적 getter는 이 특성으로 추가 그룹을 선언할 수 있다.
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class BuffTargetGroupsAttribute : Attribute
{
    public string[] Groups { get; }
    public BuffTargetGroupsAttribute(params string[] groups) => Groups = groups ?? new string[0];
}
