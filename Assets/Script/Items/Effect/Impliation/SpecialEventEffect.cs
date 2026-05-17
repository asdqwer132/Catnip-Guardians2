using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SpecialEventEffect", menuName = "Game/Item Effect/Special Event")]
public class SpecialEventEffect : ItemEffectData
{
    public UnityEvent onExecute;

    public override void Execute(ItemEffectContext context)
    {
        onExecute?.Invoke();

        Debug.Log(context.itemData.itemName + " 특수 이벤트 실행");
    }
}