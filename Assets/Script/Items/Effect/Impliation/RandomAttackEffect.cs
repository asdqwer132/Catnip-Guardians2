//using UnityEngine;

//[CreateAssetMenu(fileName = "RandomAttackEffect", menuName = "Game/Item Effect/Random Attack")]
//public class RandomAttackEffect : ItemEffectData
//{
//    public GameObject attackPrefab;
//    public int attackCount = 3;
//    public float radius = 5f;
//    public float damage = 10f;

//    public override void Execute(ItemEffectContext context)
//    {
//        if (attackPrefab == null)
//            return;

//        for (int i = 0; i < attackCount; i++)
//        {
//            Vector2 randomCircle = Random.insideUnitCircle * radius;

//            Vector3 randomPosition = context.usePosition + new Vector3(
//                randomCircle.x,
//                0f,
//                randomCircle.y
//            );

//            GameObject obj = Instantiate(
//                attackPrefab,
//                randomPosition,
//                Quaternion.identity
//            );

//            DamageArea damageArea = obj.GetComponent<DamageArea>();

//            if (damageArea != null)
//            {
//                damageArea.damage = damage;
//                damageArea.owner = context.owner;
//            }
//        }

//        Debug.Log(context.itemData.itemName + " 랜덤 공격 실행");
//    }
//}