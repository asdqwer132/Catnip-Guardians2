using UnityEngine;

[System.Serializable]
public class EnemyPatternRuntime
{
    public EnemyPatternInfo info;
    public float cooldownTimer;
    public bool consumed;

    public EnemyPatternRuntime(EnemyPatternInfo info)
    {
        this.info = info;
        cooldownTimer = 0f;
        consumed = false;
    }

    public void Tick(float deltaTime)
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= deltaTime;
    }

    public void StartCooldown()
    {
        if (info == null)
            return;

        cooldownTimer = Mathf.Max(0f, info.cooldown);

        if (info.consumeOnce)
            consumed = true;
    }
}
