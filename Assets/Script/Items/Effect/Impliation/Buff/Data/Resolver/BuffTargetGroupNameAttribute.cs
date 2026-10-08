using System;
using UnityEngine;

// 그룹은 문자열로 저장해 기존 에셋과 사용자 정의 이름을 유지한다.
public sealed class BuffTargetGroupNameAttribute : PropertyAttribute
{
    public string EmptyLabel { get; }
    public BuffTargetGroupNameAttribute(string emptyLabel = "(그룹 없음)") => EmptyLabel = emptyLabel;
}

// Inspector는 IBuffTarget 구현 타입의 선언과 현재 씬의 그룹을 자동 수집한다.
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class BuffTargetGroupsAttribute : Attribute
{
    public string[] Groups { get; }
    public BuffTargetGroupsAttribute(params string[] groups) => Groups = groups ?? new string[0];
}
