using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전달할 데이터의 자료형과 이름입니다. 실제 키/데이터 형식은 사용하는 쪽에서 정의합니다.
/// 같은 이름이라도 자료형이 다르면 다른 데이터이며, 이름은 대소문자를 구분합니다.
/// </summary>
public sealed class SceneDataKey<T>
{
    public string Name { get; }

    public SceneDataKey(string name)
    {
        if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("데이터 키의 이름이 필요합니다.",nameof(name));
        Name=name.Trim();
    }
}

/// <summary>
/// 모든 씬에서 공유하는 런타임 데이터 캐리어입니다. 컴포넌트 추가나 코어 씬 로딩 없이 사용합니다.
/// 씬 전환/언로드에는 유지되고, 새 Play 실행 또는 Clear()로 비웁니다. 파일 저장 기능은 아닙니다.
/// 참조형 데이터는 복제하지 않습니다. 씬 오브젝트 참조의 유효 기간은 호출하는 쪽에서 관리해야 합니다.
/// </summary>
public static class SceneDataCarrier
{
    private static readonly Dictionary<(Type type,string name),object> values=new Dictionary<(Type,string),object>();
    public static int Count => values.Count;

    // 도메인 리로드를 끈 에디터에서도 이전 Play의 데이터가 다음 Play에 남지 않습니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay() => Clear();

    /// <summary>넣기. 같은 키에 다시 넣으면 이전 값을 교체합니다.</summary>
    public static void Put<T>(SceneDataKey<T> key,T data) => values[Identity(key)]=data;

    /// <summary>읽기. 데이터는 캐리어에 그대로 남습니다. 없으면 false와 default(T)를 반환합니다.</summary>
    public static bool TryGet<T>(SceneDataKey<T> key,out T data)
    {
        if(values.TryGetValue(Identity(key),out var value)) { data=(T)value; return true; }
        data=default; return false;
    }

    /// <summary>한 번만 꺼내기. 읽기에 성공하면 캐리어에서 해당 데이터를 제거합니다.</summary>
    public static bool TryTake<T>(SceneDataKey<T> key,out T data)
    {
        var identity=Identity(key);
        if(values.TryGetValue(identity,out var value))
        { data=(T)value; values.Remove(identity); return true; }
        data=default; return false;
    }

    public static bool Remove<T>(SceneDataKey<T> key) => values.Remove(Identity(key));
    public static void Clear() => values.Clear();

    private static (Type type,string name) Identity<T>(SceneDataKey<T> key)
    {
        if(key==null) throw new ArgumentNullException(nameof(key));
        return (typeof(T),key.Name);
    }
}

/* 사용 양식 — 실제 데이터 클래스와 키는 나중에 작성합니다.

// 공용 키 선언 (MyData는 직접 만드는 데이터 클래스/구조체입니다.)
public static readonly SceneDataKey<MyData> MyKey = new SceneDataKey<MyData>("my-data");

// 보내는 씬
SceneDataCarrier.Put(MyKey, myData);

// 받는 씬: 일반 읽기 또는 한 번만 꺼내기 중 선택
if (SceneDataCarrier.TryGet(MyKey, out var data)) { ... }
if (SceneDataCarrier.TryTake(MyKey, out var data)) { ... }

// 새 게임 등 데이터를 버릴 시점에 직접 호출
SceneDataCarrier.Remove(MyKey);
SceneDataCarrier.Clear();
*/
