using System.Collections.Generic;
using UnityEngine;

// 다른 프리팹 런타임도 이 인터페이스를 구현하면 같은 연출과 기즈모를 사용할 수 있습니다.
public interface IPrefabVisualLifetime
{
    bool TryGetVisualLifetime(out float remaining);
    Transform ControlledRangeVisual { get; }
}

public interface IPrefabRangeProvider
{
    void CollectPrefabRanges(List<PrefabVisualRange> results);
}
