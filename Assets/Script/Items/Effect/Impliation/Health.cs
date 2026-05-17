using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHp = 100f;
    public float currentHp = 100f;

    public void Heal(float amount)
    {
        currentHp += amount;
        currentHp = Mathf.Min(currentHp, maxHp);
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Max(currentHp, 0f);
    }
}