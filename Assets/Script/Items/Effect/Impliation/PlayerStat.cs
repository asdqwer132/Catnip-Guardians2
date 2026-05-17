using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    [Header("Base Stat")]
    public EffectStat baseStat = new EffectStat();

    [Header("Bonus Stat")]
    public EffectStat bonusStat = new EffectStat();

    public EffectStat TotalStat
    {
        get
        {
            EffectStat total = new EffectStat();

            if (baseStat != null)
                total.Add(baseStat);

            if (bonusStat != null)
                total.Add(bonusStat);

            return total;
        }
    }

    public void AddBonus(EffectStat bonus)
    {
        if (bonus == null)
            return;

        if (bonusStat == null)
            bonusStat = new EffectStat();

        bonusStat.Add(bonus);
    }

    public void ResetBonus()
    {
        if (bonusStat == null)
            bonusStat = new EffectStat();

        bonusStat.Reset();
    }
}