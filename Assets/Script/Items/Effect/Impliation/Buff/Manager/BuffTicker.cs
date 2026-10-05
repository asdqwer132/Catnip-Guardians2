public class BuffTicker
{
    private readonly BuffStorage storage;

    public BuffTicker(BuffStorage storage)
    {
        this.storage = storage;
    }

    public bool Tick(float deltaTime)
    {
        if (storage == null)
            return false;

        bool changed = false;

        // 시간제 버프만 매 프레임 처리한다.
        for (int i = storage.timedBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = storage.timedBuffs[i];

            if (buff == null)
            {
                storage.timedBuffs.RemoveAt(i);
                storage.normalBuffs.Remove(null);
                storage.activeBuffs.Remove(null);
                changed = true;
                continue;
            }

            buff.Tick(deltaTime);

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
            for (int i = storage.useCountBuffs.Count - 1; i >= 0; i--)
            {
                ActiveBuff buff = storage.useCountBuffs[i];
                if (buff != null && !buff.IsExpired)
                    continue;

                if (buff == null)
                {
                    storage.useCountBuffs.RemoveAt(i);
                    storage.normalBuffs.Remove(null);
                    storage.activeBuffs.Remove(null);
                }
                else
                {
                    storage.RemoveBuff(buff);
                }
                changed = true;
            }
        }

        storage.RemoveNullRegisters();
        return changed;
    }
}
