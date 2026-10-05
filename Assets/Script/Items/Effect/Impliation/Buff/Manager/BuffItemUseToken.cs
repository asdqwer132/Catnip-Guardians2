using System.Collections.Generic;

// BeginItemUse가 반환하는 토큰. 같은 토큰을 다시 완료해도 중복 차감하지 않습니다.
public struct BuffItemUseToken
{
    internal readonly BuffItemUseSession session;
    internal readonly ulong id;

    internal BuffItemUseToken(BuffItemUseSession session, ulong id)
    {
        this.session = session;
        this.id = id;
    }
}

internal sealed class BuffItemUseSession
{
    internal BuffManager manager;
    internal ulong id;
    internal bool active;
    internal readonly List<BuffUseCandidate> candidates = new List<BuffUseCandidate>(16);
    internal readonly HashSet<ActiveBuff> appliedBuffs = new HashSet<ActiveBuff>();

    internal void Clear()
    {
        candidates.Clear();
        appliedBuffs.Clear();
        active = false;
        manager = null;
    }
}

internal struct BuffUseCandidate
{
    internal ActiveBuff buff;
    internal ulong registrationVersion;
    internal bool consumeOnComplete;
}
