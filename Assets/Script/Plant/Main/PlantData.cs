using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public float spawnWeight = 1f;
}

[System.Serializable]
public class PlantBuyCost
{
    public CurrencyType currencyType;
    public int amount;
}

[CreateAssetMenu(menuName = "Game/Plant Data")]
public class PlantData : ScriptableObject
{
    public string plantName;
    public Sprite plantSprite;

    [Header("Reward")]
    public CurrencyType rewardType;
    public int rewardAmount;

    [Header("Stat")]
    public float maxHP = 100f;
    public float growTime = 60f;

    [Header("Buy")]
    public bool unlockedByDefault;
    public PlantBuyCost[] buyCosts;

    [Header("Spawn Enemies")]
    public EnemySpawnInfo[] enemies;
}