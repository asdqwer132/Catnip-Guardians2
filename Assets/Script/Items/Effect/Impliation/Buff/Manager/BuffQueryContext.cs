public class BuffQueryContext
{
    public ItemData itemData;
    public EquipmentBag bag;
    public Enemy enemy;
    public EnemySpawner enemySpawner;
    public Player player;

    public static BuffQueryContext ForItem(ItemData itemData, EquipmentBag bag)
    {
        return new BuffQueryContext
        {
            itemData = itemData,
            bag = bag
        };
    }

    public static BuffQueryContext ForEnemy(Enemy enemy)
    {
        return new BuffQueryContext
        {
            enemy = enemy
        };
    }

    public static BuffQueryContext ForEnemySpawner(EnemySpawner enemySpawner)
    {
        return new BuffQueryContext
        {
            enemySpawner = enemySpawner
        };
    }

    public static BuffQueryContext ForPlayer(Player player)
    {
        return new BuffQueryContext
        {
            player = player
        };
    }
}