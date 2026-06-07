using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatModifier", menuName = "Game/Buff Modifier/Player Stat")]
public class PlayerStatModifier : BuffModifier
{
    [Header("Move")]
    public float addMoveSpeed;
    public float multiplyMoveSpeed = 1f;

    [Header("Max Move")]
    public float addMaxMoveSpeed;
    public float multiplyMaxMoveSpeed = 1f;

    [Header("Range")]
    public float addMinRange;
    public float multiplyMinRange = 1f;

    public float addMaxRange;
    public float multiplyMaxRange = 1f;

    private void OnValidate()
    {
        targetStatTypeName = nameof(PlayerStat);

        multiplyMoveSpeed = Mathf.Max(0f, multiplyMoveSpeed);
        multiplyMaxMoveSpeed = Mathf.Max(0f, multiplyMaxMoveSpeed);
        multiplyMinRange = Mathf.Max(0f, multiplyMinRange);
        multiplyMaxRange = Mathf.Max(0f, multiplyMaxRange);
    }

    public override void ApplyTo(object stat, int stack, BuffQueryContext query)
    {
        PlayerStat playerStat = stat as PlayerStat;

        if (playerStat == null)
            return;

        stack = Mathf.Max(1, stack);

        playerStat.moveSpeed =
            playerStat.moveSpeed * Mathf.Pow(multiplyMoveSpeed, stack) +
            addMoveSpeed * stack;

        playerStat.maxMoveSpeed =
            playerStat.maxMoveSpeed * Mathf.Pow(multiplyMaxMoveSpeed, stack) +
            addMaxMoveSpeed * stack;

        playerStat.minRange =
            playerStat.minRange * Mathf.Pow(multiplyMinRange, stack) +
            addMinRange * stack;

        playerStat.maxRange =
            playerStat.maxRange * Mathf.Pow(multiplyMaxRange, stack) +
            addMaxRange * stack;

        playerStat.Clamp();
    }
}