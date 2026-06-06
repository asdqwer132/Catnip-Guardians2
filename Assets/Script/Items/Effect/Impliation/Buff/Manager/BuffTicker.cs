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

        for (int i = storage.activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = storage.activeBuffs[i];

            if (buff == null)
            {
                storage.activeBuffs.RemoveAt(i);
                changed = true;
                continue;
            }

            buff.Tick(deltaTime);

            if (buff.IsExpired)
            {
                storage.activeBuffs.RemoveAt(i);
                changed = true;
            }
        }

        storage.RemoveNullRegisters();
        return changed;
    }
}
