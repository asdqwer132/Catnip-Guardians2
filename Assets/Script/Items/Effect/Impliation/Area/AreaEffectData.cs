using UnityEngine;

// 기존 ReactiveGroundArea 런타임을 재사용합니다. 종류 및 생명주기는 Definition으로 설정합니다.
[CreateAssetMenu(fileName = "AreaEffect", menuName = "GameData/Items/Effects/Attack/Defined Area")]
public sealed class AreaEffectData : ReactiveGroundEffect { }
