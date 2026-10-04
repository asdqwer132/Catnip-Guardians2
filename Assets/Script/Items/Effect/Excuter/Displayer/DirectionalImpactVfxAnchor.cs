using UnityEngine;

// 이펙트가 자신의 기존 수명 처리로 사라지면 방향용 부모도 정리한다.
public class DirectionalImpactVfxAnchor : MonoBehaviour
{
    private ImpactVfxInstance impact;

    public void Init(ImpactVfxInstance impactInstance)
    {
        impact = impactInstance;
    }

    private void LateUpdate()
    {
        if (impact == null)
            Destroy(gameObject);
    }
}
