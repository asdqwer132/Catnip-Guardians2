using UnityEngine;

public class EnemyPatternRuntime
{
    public EnemyPatternEntry Entry { get; private set; }
    public float CooldownTimer { get; private set; }
    public bool Consumed { get; private set; }

    public EnemyPatternRuntime(EnemyPatternEntry entry)
    {
        Entry = entry;
    }

    public void Tick(float deltaTime)
    {
        if (CooldownTimer <= 0f)
            return;

        CooldownTimer -= deltaTime;

        if (CooldownTimer < 0f)
            CooldownTimer = 0f;
    }

    public void StartCooldown()
    {
        if (Entry == null)
            return;

        CooldownTimer = Mathf.Max(0f, Entry.cooldown);

        if (Entry.consumeOnce)
            Consumed = true;
    }

    public void Reset()
    {
        CooldownTimer = 0f;
        Consumed = false;
    }
}
