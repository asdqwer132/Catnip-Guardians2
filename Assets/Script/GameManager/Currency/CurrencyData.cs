using UnityEngine;

// 재화의 표시 정보만 보관합니다. 수량은 CurrencyManager/게임 통계가 관리합니다.
[CreateAssetMenu(fileName = "CurrencyData", menuName = "Currency/Display Data")]
public sealed class CurrencyData : DefinedDataCollection<CurrencyType>
{
}
