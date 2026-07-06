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

        // 일반 버프만 시간 경과와 만료를 처리합니다.
        // 무한 버프는 Tick 대상이 아닙니다.
        for (int i = storage.normalBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = storage.normalBuffs[i];

            if (buff == null)
            {
                storage.normalBuffs.RemoveAt(i);
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

        // 무한 버프에는 만료 처리를 하지 않고 null만 정리합니다.
        for (int i = storage.infiniteBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = storage.infiniteBuffs[i];

            if (buff != null)
                continue;

            storage.infiniteBuffs.RemoveAt(i);
            storage.activeBuffs.Remove(null);
            changed = true;
        }

        storage.RemoveNullRegisters();
        return changed;
    }
}
