public class BuffTicker
{
    private readonly BuffStorage storage;
    private readonly System.Collections.Generic.List<ActiveBuff> updateBuffer = new System.Collections.Generic.List<ActiveBuff>();
    private bool ticking;

    public BuffTicker(BuffStorage storage)
    {
        this.storage = storage;
    }

    public bool Tick(float deltaTime)
    {
        if (storage == null || ticking)
            return false;
        ticking = true;
        try { return TickInternal(deltaTime); }
        finally { updateBuffer.Clear(); ticking = false; }
    }

    private bool TickInternal(float deltaTime)
    {

        bool changed = false;

        // 시간제 버프만 매 프레임 처리한다.
        updateBuffer.AddRange(storage.timedBuffs);
        for (int i = 0; i < updateBuffer.Count; i++)
        {
            ActiveBuff buff = updateBuffer[i];

            if (buff == null)
            {
                storage.timedBuffs.Remove(null);
                storage.normalBuffs.Remove(null);
                storage.activeBuffs.Remove(null);
                changed = true;
                continue;
            }

            if (buff.StorageOwner != storage) continue;
            if (!IsEnemyStatusTimerStopped(buff)) buff.Tick(deltaTime);

            if (!buff.IsExpired)
                continue;

            storage.RemoveBuff(buff);
            changed = true;
        }

        // 외부 코드가 ActiveBuff.ConsumeUse()를 직접 호출한 경우도 만료를 정리한다.
        // 정상 아이템 실행 경로는 EndItemUse에서 즉시 제거한다.
        if (storage.HasExpiredUseCounts)
        {
            storage.HasExpiredUseCounts = false;
            updateBuffer.Clear();
            updateBuffer.AddRange(storage.useCountBuffs);
            for (int i = 0; i < updateBuffer.Count; i++)
            {
                ActiveBuff buff = updateBuffer[i];
                if (buff != null && buff.StorageOwner != storage) continue;
                if (buff != null && !buff.IsExpired)
                    continue;

                if (buff == null)
                {
                    storage.useCountBuffs.Remove(null);
                    storage.normalBuffs.Remove(null);
                    storage.activeBuffs.Remove(null);
                }
                else
                {
                    storage.RemoveBuff(buff, BuffRemovalReason.Consumed);
                }
                changed = true;
            }
        }

        storage.RemoveNullRegisters();
        return changed;
    }

    private static bool IsEnemyStatusTimerStopped(ActiveBuff buff)
    {
        if (!TimeStopRuntime.IsStopped(TimeStopTargets.EnemyStatusTimers) || buff.target == null) return false;
        UnityEngine.Component component = buff.target.targetObject as UnityEngine.Component;
        UnityEngine.GameObject host = buff.target.targetObject as UnityEngine.GameObject;
        return component != null ? component.GetComponentInParent<Enemy>() != null :
            (host != null && host.GetComponentInParent<Enemy>() != null);
    }
}
