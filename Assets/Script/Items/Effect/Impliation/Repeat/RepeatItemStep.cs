using System;
using UnityEngine;

[Serializable]
public sealed class RepeatItemStep
{
    [Tooltip("이 묶음의 아이템을 동시에 사용합니다. 같은 아이템을 여러 칸에 넣으면 그 개수만큼 동시에 사용합니다.")]
    public ItemData[] items;

    [Min(0f)]
    [Tooltip("이 묶음을 사용한 뒤 다음 묶음까지 기다리는 시간입니다. 마지막 사용 뒤에는 기다리지 않습니다.")]
    public float intervalAfter = 0.2f;
}
