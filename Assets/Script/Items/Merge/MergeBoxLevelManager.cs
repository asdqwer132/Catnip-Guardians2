using System;
using UnityEngine;

[Serializable]
public sealed class MergeBoxLevel
{
    [Min(1)] public int minimumLevel = 1;
    public MergeBoxBindings box;
}

// 시리즈 루트 하나에 하나씩 둡니다. Box1/Box2의 물리 영역만 바꾸고 재료/레시피/풀은 기존 board가 유지합니다.
[DisallowMultipleComponent, DefaultExecutionOrder(-300)]
public sealed class MergeBoxLevelManager : MonoBehaviour
{
    [Tooltip("이 시리즈의 Box1, Box2 등을 연결합니다. 각 박스에는 자체 Spawn Point와 Drop Zone이 있어야 합니다.")]
    public MergeBoxLevel[] levels = Array.Empty<MergeBoxLevel>();
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField] private MergegameManager board;

    public int CurrentLevel => currentLevel;
    public int ActiveBoxLevel { get; private set; }
    public MergeBoxBindings CurrentBox { get; private set; }
    public MergegameManager Board => board;
    public event Action<MergeBoxLevelManager> BoxChanged;

    private void Awake() => TrySetLevel(currentLevel);

    public void BindBoard(MergegameManager value)
    {
        board = value;
        TrySetLevel(currentLevel);
    }

    // UnityEvent에는 SetLevel / Upgrade를, 호출 성공 여부가 필요하면 Try 함수를 사용합니다.
    public void SetLevel(int value) => TrySetLevel(value);
    public void Upgrade() => TryUpgrade();

    public bool TryUpgrade()
    {
        int next = int.MaxValue;
        bool found = false;
        if (levels != null) foreach (var entry in levels)
            if (entry?.box != null && entry.minimumLevel > currentLevel && (!found || entry.minimumLevel < next))
            { next = entry.minimumLevel; found = true; }
        return found && TrySetLevel(next);
    }

    public bool TrySetLevel(int value)
    {
        value = Mathf.Max(1, value);
        if (!TryResolve(value, out var target)) return false;
        var previous = CurrentBox;
        currentLevel = value;
        CurrentBox = target.box;
        ActiveBoxLevel = target.minimumLevel;

        if (board != null)
        {
            // 매니저를 끄거나 재시작하지 않아 재고, 토글, 투입 타이머는 그대로 남습니다.
            board.spawnPoint = CurrentBox.spawnPoint;
            board.dropZone = CurrentBox.dropZone;
            if (previous != null && previous != CurrentBox)
                board.RemapLayout(CurrentBox.transform.localToWorldMatrix * previous.transform.worldToLocalMatrix);
        }
        foreach (var entry in levels)
            if (entry?.box != null && entry.box != CurrentBox) entry.box.gameObject.SetActive(false);
        CurrentBox.gameObject.SetActive(true);
        CurrentBox.dropZone.RefreshLine();
        if (previous != CurrentBox) BoxChanged?.Invoke(this);
        return true;
    }

    private bool TryResolve(int value, out MergeBoxLevel target)
    {
        target = null;
        MergeBoxLevel first = null;
        if (levels == null) return false;
        for (int i = 0; i < levels.Length; i++)
        {
            var entry = levels[i];
            if (entry?.box == null) continue;
            var box = entry.box;
            // 다른 시리즈나 부모 루트를 끄지 않도록 이 루트 아래의 박스만 관리합니다.
            if (entry.minimumLevel < 1 || box.transform == transform || !box.transform.IsChildOf(transform) ||
                box.spawnPoint == null || !box.spawnPoint.IsChildOf(box.transform) ||
                box.dropZone == null || !box.dropZone.transform.IsChildOf(box.transform)) return false;
            for (int j = 0; j < i; j++)
            {
                var other = levels[j];
                if (other?.box == null) continue;
                if (entry.minimumLevel == other.minimumLevel || box.transform.IsChildOf(other.box.transform) ||
                    other.box.transform.IsChildOf(box.transform)) return false;
            }
            if (first == null || entry.minimumLevel < first.minimumLevel) first = entry;
            if (entry.minimumLevel <= value && (target == null || entry.minimumLevel > target.minimumLevel)) target = entry;
        }
        if (target == null) target = first;
        return target != null;
    }
}
