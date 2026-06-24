using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStatData", menuName = "GameData/Enemy/Enemy Stat Data")]
public class EnemyStatData : ScriptableObject
{
    [Header("Class")]
    public string enemyClass = "";

    [Header("Move")]
    public float speed = 2f;

    [Header("Health")]
    public float maxHp = 10f;

    [Header("Attack")]
    public float damage = 5f;

    [Header("Attack Range")]
    public float minAttackRange = 1f;
    public float maxAttackRange = 2f;

    [Header("Attack Cooldown")]
    public float minAttackCooldown = 0.8f;
    public float maxAttackCooldown = 1.2f;

    [Header("Reward")]
    public Cost[] reward;

    [Header("Grow EXP")]
    public float minGrowEx = 8f;
    public float maxGrowEx = 12f;

    private void OnValidate()
    {
        if (minAttackRange > maxAttackRange)
            Swap(ref minAttackRange, ref maxAttackRange);

        if (minAttackCooldown > maxAttackCooldown)
            Swap(ref minAttackCooldown, ref maxAttackCooldown);

        if (minGrowEx > maxGrowEx)
            Swap(ref minGrowEx, ref maxGrowEx);

        minAttackRange = Mathf.Max(0f, minAttackRange);
        maxAttackRange = Mathf.Max(0f, maxAttackRange);

        minAttackCooldown = Mathf.Max(0f, minAttackCooldown);
        maxAttackCooldown = Mathf.Max(0f, maxAttackCooldown);

        minGrowEx = Mathf.Max(0f, minGrowEx);
        maxGrowEx = Mathf.Max(0f, maxGrowEx);
    }

    private void Swap(ref float a, ref float b)
    {
        float temp = a;
        a = b;
        b = temp;
    }
    public void CreateStatTo(EnemyStat stat)
    {
        if (stat == null)
            return;

        stat.speed = speed;
        stat.maxHp = maxHp;
        stat.damage = damage;

        stat.attackRange = Random.Range(minAttackRange, maxAttackRange);
        stat.attackCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
        stat.growEx = Random.Range(minGrowEx, maxGrowEx);

        stat.Clamp();
    }
    public EnemyStat CreateStat()
    {
        EnemyStat stat = new EnemyStat();

        stat.speed = speed;
        stat.maxHp = maxHp;
        stat.damage = damage;

        stat.attackRange = Random.Range(minAttackRange, maxAttackRange);
        stat.attackCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);

        // EnemyStat 안에 growEx가 있다면 이거 사용
        stat.growEx = Random.Range(minGrowEx, maxGrowEx);

        stat.Clamp();

        return stat;
    }

    // EnemyStat 안에 growEx가 없다면 Enemy 쪽에서 이걸 따로 호출해서 쓰면 됨
    public float GetRandomGrowEx()
    {
        return Random.Range(minGrowEx, maxGrowEx);
    }
}