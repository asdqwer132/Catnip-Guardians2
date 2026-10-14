// 성공하지 못한 아이템 사용은 다음 소환에서 다시 시도할 수 있습니다.
// 실행 중 상태가 바뀌거나 소환이 재진입해도 진행 중인 반응은 중복 시작하지 않습니다.
public sealed class SummonGroupActivationGate
{
    private bool matches, fired, pending;
    public void Observe(bool value)
    {
        matches = value;
        if (!matches) fired = false;
    }
    public bool TryBegin(bool value, SummonGroupTriggerMode mode)
    {
        Observe(value);
        if (!matches || pending || (mode == SummonGroupTriggerMode.OnConditionReached && fired)) return false;
        pending = true;
        return true;
    }
    public void Complete(bool succeeded)
    {
        pending = false;
        if (succeeded && matches) fired = true;
    }
}
